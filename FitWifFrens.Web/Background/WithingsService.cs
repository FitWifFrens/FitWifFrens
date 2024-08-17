using FitWifFrens.Data;
using Hangfire;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Web.Background
{
    // TODO: need to handle delete
    public class WithingsService
    {
        private readonly record struct ResponseJsonDocument(HttpResponseMessage Response, JsonDocument JsonDocument) : IDisposable
        {
            public void Dispose()
            {
                Response.Dispose();
                JsonDocument.Dispose();
            }
        }

        private readonly BackgroundConfiguration _backgroundConfiguration;
        private readonly DataContext _dataContext;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly HttpClient _httpClient;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<WithingsService> _logger;

        private readonly ResiliencePipeline<ResponseJsonDocument> _resiliencePipeline;

        public WithingsService(BackgroundConfiguration backgroundConfiguration, DataContext dataContext, IBackgroundJobClient backgroundJobClient,
            IHttpClientFactory httpClientFactory, RefreshTokenService refreshTokenService, TelemetryClient telemetryClient, ILogger<WithingsService> logger)
        {
            _backgroundConfiguration = backgroundConfiguration;
            _dataContext = dataContext;
            _backgroundJobClient = backgroundJobClient;
            _httpClient = httpClientFactory.CreateClient();
            _refreshTokenService = refreshTokenService;
            _telemetryClient = telemetryClient;
            _logger = logger;

            _resiliencePipeline = new ResiliencePipelineBuilder<ResponseJsonDocument>()
                .AddRetry(new RetryStrategyOptions<ResponseJsonDocument>
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<ResponseJsonDocument>()
                        .HandleResult(rd => !rd.Response.IsSuccessStatusCode)
                        .HandleResult(rd => rd.JsonDocument.RootElement.GetProperty("status").GetInt32() != 0),
                })
                .AddRetry(new RetryStrategyOptions<ResponseJsonDocument>
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<ResponseJsonDocument>()
                        .HandleResult(rd => rd.Response.StatusCode == HttpStatusCode.Unauthorized)
                        .HandleResult(rd => rd.JsonDocument.RootElement.GetProperty("status").GetInt32() == 401),

                    OnRetry = async args =>
                    {
                        if (args.Context.Properties.TryGetValue(new ResiliencePropertyKey<string>("UserId"), out var userId))
                        {
                            await _refreshTokenService.RefreshWithingsToken(userId, args.Context.CancellationToken);
                        }
                        else
                        {
                            throw new Exception("70a8eebb-0af3-4797-acf4-fd5bd457bd75");
                        }
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(10))
                .Build();
        }

        public async Task UpdateWebhook(string withingsId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_backgroundConfiguration.CallbackUrl))
            {
                try
                {
                    var userLogin = await _dataContext.UserLogins.Include(ul => ul.User.Tokens).SingleOrDefaultAsync(ul => ul.LoginProvider == "Withings" && ul.ProviderKey == withingsId, cancellationToken: cancellationToken);

                    if (userLogin != null)
                    {
                        await UpdateWebhook(userLogin.User, cancellationToken);
                    }
                }
                catch (Exception exception)
                {
                    _telemetryClient.TrackException(exception);
                    throw;
                }
            }
        }

        public async Task UpdateWebhooks(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_backgroundConfiguration.CallbackUrl))
            {
                try
                {
                    foreach (var user in await _dataContext.Users.Where(u => u.Logins.Any(l => l.LoginProvider == "Withings")).Include(u => u.Tokens).ToListAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await UpdateWebhook(user, cancellationToken);
                    }
                }
                catch (Exception exception)
                {
                    _telemetryClient.TrackException(exception);
                    throw;
                }
            }
        }

        private async Task UpdateWebhook(User user, CancellationToken cancellationToken)
        {
            var tokens = user.Tokens.Where(t => t.LoginProvider == "Withings").ToList();

            if (tokens.Any())
            {
                foreach (var webhookSubscription in Constants.Withings.WebhookSubscriptions)
                {
                    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                    resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                    using var responseJsonDocument = await _resiliencePipeline.ExecuteAsync(async rc =>
                    {
                        // TODO: "{\"status\": ... not equal to 0
                        // TODO: remove webhook on delete?
                        using var request = new HttpRequestMessage(HttpMethod.Post, "https://wbsapi.withings.net/notify");
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "action", "subscribe" },
                            { "appli", webhookSubscription.ToString() },
                            { "callbackurl", $"{_backgroundConfiguration.CallbackUrl}/api/webhooks/withings" },
                        });
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _refreshTokenService.GetWithingsToken(user.Id, rc.CancellationToken));

                        var response = await _httpClient.SendAsync(request, cancellationToken);

                        return new ResponseJsonDocument(response, JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)));

                    }, resilienceContext);

                    ResilienceContextPool.Shared.Return(resilienceContext);
                }
            }
        }

        public async Task UpdateProviderMetricValues(string withingsId, CancellationToken cancellationToken)
        {
            try
            {
                var metricProviders = await _dataContext.MetricProviders.Where(mp => mp.ProviderName == "Withings").ToListAsync(cancellationToken); // TODO: should I do this??? or just try update??

                if (metricProviders.Any())
                {
                    var userLogin = await _dataContext.UserLogins.Include(ul => ul.User.Tokens).SingleOrDefaultAsync(ul => ul.LoginProvider == "Withings" && ul.ProviderKey == withingsId, cancellationToken: cancellationToken);

                    if (userLogin != null)
                    {
                        await UpdateProviderMetricValues(userLogin.User, cancellationToken);

                        _backgroundJobClient.Enqueue<CommitmentPeriodService>(s => s.UpdateCommitmentPeriodUserGoals(CancellationToken.None));
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }

        public async Task UpdateProviderMetricValues(CancellationToken cancellationToken)
        {
            try
            {
                var metricProviders = await _dataContext.MetricProviders.Where(mp => mp.ProviderName == "Withings").ToListAsync(cancellationToken);

                if (metricProviders.Any())
                {
                    foreach (var user in await _dataContext.Users.Where(u => u.Logins.Any(l => l.LoginProvider == "Withings")).Include(u => u.Tokens).ToListAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await UpdateProviderMetricValues(user, cancellationToken);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }

        private async Task UpdateProviderMetricValues(User user, CancellationToken cancellationToken)
        {
            var tokens = user.Tokens.Where(t => t.LoginProvider == "Withings").ToList();

            if (tokens.Any())
            {
                _telemetryClient.TrackTrace($"Updating Withings data for user {user.Id} with token {tokens.Single(t => t.Name == "access_token").Value}", SeverityLevel.Information);

                {
                    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                    resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                    // TODO: "{\"status\":401,\"body\":{},\"error\":\"XRequestID: Not provided invalid_token: The access token provided is invalid\"}"
                    using var responseJsonDocument = await _resiliencePipeline.ExecuteAsync(async rc =>
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Post, "https://wbsapi.withings.net/measure");
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "action", "getmeas" },
                            { "meastypes", "1,9" },
                            { "lastupdate", DateTime.UtcNow.AddDays(-Constants.ProviderSearchDaysBack).ToUnixTimeSeconds().ToString() },
                        });
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _refreshTokenService.GetWithingsToken(user.Id, rc.CancellationToken));

                        var response = await _httpClient.SendAsync(request, cancellationToken);

                        return new ResponseJsonDocument(response, JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)));

                    }, resilienceContext);

                    ResilienceContextPool.Shared.Return(resilienceContext);

                    foreach (var measureGroupJson in responseJsonDocument.JsonDocument.RootElement.GetProperty("body").GetProperty("measuregrps").EnumerateArray())
                    {
                        var measureGroupTime = DateTimeExs.FromUnixTimeSeconds(measureGroupJson.GetProperty("created").GetInt64(), DateTimeKind.Utc);

                        foreach (var measureJson in measureGroupJson.GetProperty("measures").EnumerateArray())
                        {
                            var measureType = measureJson.GetProperty("type").GetInt32();

                            if (measureType == 1)
                            {
                                var weightDivisor = measureJson.GetProperty("unit").GetInt32() switch
                                {
                                    -3 => 1000.0,
                                    -4 => 10000.0,
                                    _ => throw new ArgumentOutOfRangeException("unit", measureJson.GetProperty("unit").GetInt32(), "9652af49-4e0f-43d4-9b0e-a035a6fa9cf3")
                                };

                                var measureValue = Math.Round(measureJson.GetProperty("value").GetInt32() / weightDivisor, 1);

                                var userMetricProviderValue = await _dataContext.UserMetricProviderValues
                                    .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Weight" && umpv.ProviderName == "Withings" &&
                                                                  umpv.MetricType == MetricType.Value && umpv.Time == measureGroupTime, cancellationToken: cancellationToken);

                                if (userMetricProviderValue == null)
                                {
                                    _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                                    {
                                        UserId = user.Id,
                                        MetricName = "Weight",
                                        ProviderName = "Withings",
                                        MetricType = MetricType.Value,
                                        Time = measureGroupTime,
                                        Value = measureValue
                                    });

                                    await _dataContext.SaveChangesAsync(cancellationToken);
                                }
                                else if (userMetricProviderValue.Value != measureValue)
                                {
                                    userMetricProviderValue.Value = measureValue;

                                    _dataContext.Entry(userMetricProviderValue).State = EntityState.Modified;

                                    await _dataContext.SaveChangesAsync(cancellationToken);
                                }
                            }
                            else if (measureType == 9)
                            {
                                var measureValue = 1D;

                                var userMetricProviderValue = await _dataContext.UserMetricProviderValues
                                    .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Blood Pressure" && umpv.ProviderName == "Withings" &&
                                                                  umpv.MetricType == MetricType.Count && umpv.Time == measureGroupTime, cancellationToken: cancellationToken);

                                if (userMetricProviderValue == null)
                                {
                                    _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                                    {
                                        UserId = user.Id,
                                        MetricName = "Blood Pressure",
                                        ProviderName = "Withings",
                                        MetricType = MetricType.Count,
                                        Time = measureGroupTime,
                                        Value = measureValue
                                    });

                                    await _dataContext.SaveChangesAsync(cancellationToken);
                                }
                                else if (userMetricProviderValue.Value != measureValue)
                                {
                                    userMetricProviderValue.Value = measureValue;

                                    _dataContext.Entry(userMetricProviderValue).State = EntityState.Modified;

                                    await _dataContext.SaveChangesAsync(cancellationToken);
                                }
                            }
                        }
                    }
                }


                {
                    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                    resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                    using var responseJsonDocument = await _resiliencePipeline.ExecuteAsync(async rc =>
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Post, "https://wbsapi.withings.net/v2/measure");
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "action", "getworkouts" },
                            { "lastupdate", DateTime.UtcNow.AddDays(-Constants.ProviderSearchDaysBack).ToUnixTimeSeconds().ToString() },
                        });
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _refreshTokenService.GetWithingsToken(user.Id, rc.CancellationToken));

                        var response = await _httpClient.SendAsync(request, cancellationToken);

                        return new ResponseJsonDocument(response, JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)));

                    }, resilienceContext);

                    ResilienceContextPool.Shared.Return(resilienceContext);

                    foreach (var activityJson in responseJsonDocument.JsonDocument.RootElement.GetProperty("body").GetProperty("series").EnumerateArray())
                    {
                        var activityStartTime = DateTimeExs.FromUnixTimeSeconds(activityJson.GetProperty("startdate").GetInt64(), DateTimeKind.Utc);
                        var activityEndTime = DateTimeExs.FromUnixTimeSeconds(activityJson.GetProperty("enddate").GetInt64(), DateTimeKind.Utc);

                        var activityMinutes = Math.Round((activityEndTime - activityStartTime).TotalMinutes, 2);

                        var activityCategory = activityJson.GetProperty("category").GetInt32();

                        // https://github.com/zono-dev/withings-go/blob/514b8ec90158faa88e36508f778fbf7c2b03e209/withings/enum.go#L125
                        // https://help.validic.com/space/VCS/1681326286/Withings+API+Integration+for+Developers
                        if (activityCategory == 16 || activityCategory == 17 || activityCategory == 28)
                        {
                            var userMetricProviderValue = await _dataContext.UserMetricProviderValues
                                .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Workout" && umpv.ProviderName == "Withings" &&
                                                              umpv.MetricType == MetricType.Minutes && umpv.Time == activityStartTime, cancellationToken: cancellationToken);

                            if (userMetricProviderValue == null)
                            {
                                _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                                {
                                    UserId = user.Id,
                                    MetricName = "Workout",
                                    ProviderName = "Withings",
                                    MetricType = MetricType.Minutes,
                                    Time = activityStartTime,
                                    Value = activityMinutes
                                });

                                await _dataContext.SaveChangesAsync(cancellationToken);
                            }
                            else if (userMetricProviderValue.Value != activityMinutes)
                            {
                                userMetricProviderValue.Value = activityMinutes;

                                _dataContext.Entry(userMetricProviderValue).State = EntityState.Modified;

                                await _dataContext.SaveChangesAsync(cancellationToken);
                            }
                        }

                        {
                            var userMetricProviderValue = await _dataContext.UserMetricProviderValues
                                .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Exercise" && umpv.ProviderName == "Withings" &&
                                                              umpv.MetricType == MetricType.Minutes && umpv.Time == activityStartTime, cancellationToken: cancellationToken);

                            if (userMetricProviderValue == null)
                            {
                                _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                                {
                                    UserId = user.Id,
                                    MetricName = "Exercise",
                                    ProviderName = "Withings",
                                    MetricType = MetricType.Minutes,
                                    Time = activityStartTime,
                                    Value = activityMinutes
                                });

                                await _dataContext.SaveChangesAsync(cancellationToken);
                            }
                            else if (userMetricProviderValue.Value != activityMinutes)
                            {
                                userMetricProviderValue.Value = activityMinutes;

                                _dataContext.Entry(userMetricProviderValue).State = EntityState.Modified;

                                await _dataContext.SaveChangesAsync(cancellationToken);
                            }
                        }
                    }
                }
            }
        }
    }
}
