using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Web.Background
{
    public class StravaService
    {
        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<StravaService> _logger;

        private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

        public StravaService(DataContext dataContext, IHttpClientFactory httpClientFactory, RefreshTokenService refreshTokenService, TelemetryClient telemetryClient, ILogger<StravaService> logger)
        {
            _dataContext = dataContext;
            _refreshTokenService = refreshTokenService;
            _httpClient = httpClientFactory.CreateClient();
            _telemetryClient = telemetryClient;
            _logger = logger;

            _resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(r => r.StatusCode == HttpStatusCode.Unauthorized),

                    OnRetry = async args =>
                    {
                        if (args.Context.Properties.TryGetValue(new ResiliencePropertyKey<string>("UserId"), out var userId))
                        {
                            await _refreshTokenService.RefreshStravaToken(userId, args.Context.CancellationToken);
                        }
                        else
                        {
                            throw new Exception("70a8eebb-0af3-4797-acf4-fd5bd457bd75");
                        }
                    }
                })
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(r => !r.IsSuccessStatusCode)
                })
                .AddTimeout(TimeSpan.FromSeconds(10))
                .Build();
        }

        public async Task UpdateProviderMetricValues(CancellationToken cancellationToken)
        {
            try
            {
                var providerMetricValues = await _dataContext.ProviderMetricValues.Where(pmv => pmv.ProviderName == "Strava").ToListAsync(cancellationToken);

                if (providerMetricValues.Any())
                {
                    foreach (var user in await _dataContext.Users.Include(u => u.Tokens).ToListAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var tokens = user.Tokens.Where(t => t.LoginProvider == "Strava").ToList();

                        if (tokens.Any())
                        {
                            var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                            resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                            using var response = await _resiliencePipeline.ExecuteAsync(async rc =>
                            {
                                using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("https://www.strava.com/api/v3/athlete/activities", new Dictionary<string, string?>
                                {
                                    { "after", DateTime.UtcNow.AddDays(-30).ToUnixTimeSeconds().ToString() },
                                    { "per_page", "200" }
                                }));
                                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _refreshTokenService.GetStravaToken(user.Id, rc.CancellationToken));

                                return await _httpClient.SendAsync(request, cancellationToken);

                            }, resilienceContext);

                            ResilienceContextPool.Shared.Return(resilienceContext);

                            using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

                            foreach (var activityJson in responseJson.RootElement.EnumerateArray())
                            {
                                var activityTime = activityJson.GetProperty("start_date").GetDateTime();
                                var activityMinutes = Math.Round(activityJson.GetProperty("elapsed_time").GetInt32() / 60.0, 2);

                                var activityType = activityJson.GetProperty("type").GetString();

                                if (activityType == "Run" || activityType == "VirtualRun")
                                {
                                    var userProviderMetricValue = await _dataContext.UserProviderMetricValues
                                        .SingleOrDefaultAsync(upmv => upmv.UserId == user.Id && upmv.ProviderName == "Strava" && upmv.MetricName == "Running" &&
                                                                      upmv.MetricType == MetricType.Minutes && upmv.Time == activityTime, cancellationToken: cancellationToken);

                                    if (userProviderMetricValue == null)
                                    {
                                        _dataContext.UserProviderMetricValues.Add(new UserProviderMetricValue
                                        {
                                            UserId = user.Id,
                                            ProviderName = "Strava",
                                            MetricName = "Running",
                                            MetricType = MetricType.Minutes,
                                            Time = activityTime,
                                            Value = activityMinutes
                                        });

                                        await _dataContext.SaveChangesAsync(cancellationToken);
                                    }
                                    else if (userProviderMetricValue.Value != activityMinutes)
                                    {
                                        userProviderMetricValue.Value = activityMinutes;

                                        _dataContext.Entry(userProviderMetricValue).State = EntityState.Modified;

                                        await _dataContext.SaveChangesAsync(cancellationToken);
                                    }
                                }
                                else if (activityType == "Ride" || activityType == "VirtualRide")
                                {

                                }
                                else if (activityType == "Workout" || activityType == "WeightTraining" || activityType == "Yoga")
                                {
                                    var userProviderMetricValue = await _dataContext.UserProviderMetricValues
                                        .SingleOrDefaultAsync(upmv => upmv.UserId == user.Id && upmv.ProviderName == "Strava" && upmv.MetricName == "Workout" &&
                                                                      upmv.MetricType == MetricType.Minutes && upmv.Time == activityTime, cancellationToken: cancellationToken);

                                    if (userProviderMetricValue == null)
                                    {
                                        _dataContext.UserProviderMetricValues.Add(new UserProviderMetricValue
                                        {
                                            UserId = user.Id,
                                            ProviderName = "Strava",
                                            MetricName = "Workout",
                                            MetricType = MetricType.Minutes,
                                            Time = activityTime,
                                            Value = activityMinutes
                                        });

                                        await _dataContext.SaveChangesAsync(cancellationToken);
                                    }
                                    else if (userProviderMetricValue.Value != activityMinutes)
                                    {
                                        userProviderMetricValue.Value = activityMinutes;

                                        _dataContext.Entry(userProviderMetricValue).State = EntityState.Modified;

                                        await _dataContext.SaveChangesAsync(cancellationToken);
                                    }
                                }

                                {
                                    var userProviderMetricValue = await _dataContext.UserProviderMetricValues
                                        .SingleOrDefaultAsync(upmv => upmv.UserId == user.Id && upmv.ProviderName == "Strava" && upmv.MetricName == "Exercise" &&
                                                                      upmv.MetricType == MetricType.Minutes && upmv.Time == activityTime, cancellationToken: cancellationToken);

                                    if (userProviderMetricValue == null)
                                    {
                                        _dataContext.UserProviderMetricValues.Add(new UserProviderMetricValue
                                        {
                                            UserId = user.Id,
                                            ProviderName = "Strava",
                                            MetricName = "Exercise",
                                            MetricType = MetricType.Minutes,
                                            Time = activityTime,
                                            Value = activityMinutes
                                        });

                                        await _dataContext.SaveChangesAsync(cancellationToken);
                                    }
                                    else if (userProviderMetricValue.Value != activityMinutes)
                                    {
                                        userProviderMetricValue.Value = activityMinutes;

                                        _dataContext.Entry(userProviderMetricValue).State = EntityState.Modified;

                                        await _dataContext.SaveChangesAsync(cancellationToken);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }
    }
}
