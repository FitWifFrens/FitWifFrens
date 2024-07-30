using FitWifFrens.Data;
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
    public class MicrosoftService
    {
        private readonly DataContext _dataContext;
        private readonly TimeProvider _timeProvider;
        private readonly HttpClient _httpClient;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<MicrosoftService> _logger;

        private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

        public MicrosoftService(DataContext dataContext, TimeProvider timeProvider, IHttpClientFactory httpClientFactory, RefreshTokenService refreshTokenService, TelemetryClient telemetryClient, ILogger<MicrosoftService> logger)
        {
            _dataContext = dataContext;
            _timeProvider = timeProvider;
            _refreshTokenService = refreshTokenService;
            _httpClient = httpClientFactory.CreateClient();
            _telemetryClient = telemetryClient;
            _logger = logger;

            _resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(r => !r.IsSuccessStatusCode)
                })
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(r => r.StatusCode == HttpStatusCode.Unauthorized),

                    OnRetry = async args =>
                    {
                        if (args.Context.Properties.TryGetValue(new ResiliencePropertyKey<string>("UserId"), out var userId))
                        {
                            await _refreshTokenService.RefreshMicrosoftToken(userId, args.Context.CancellationToken);
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

        public async Task UpdateProviderMetricValues(CancellationToken cancellationToken)
        {
            try
            {
                var metricProviders = await _dataContext.MetricProviders.Where(mp => mp.ProviderName == "Microsoft").ToListAsync(cancellationToken);

                if (metricProviders.Any())
                {
                    var date = _timeProvider.GetUtcNow().DateTime.ConvertTimeFromUtc().Date.SpecifyUtcKind();

                    foreach (var user in await _dataContext.Users.Where(u => u.Logins.Any(l => l.LoginProvider == "Microsoft")).Include(u => u.Tokens).ToListAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var tokens = user.Tokens.Where(t => t.LoginProvider == "Microsoft").ToList();

                        if (tokens.Any())
                        {
                            _telemetryClient.TrackTrace($"Updating Microsoft data for user {user.Id} with token {tokens.Single(t => t.Name == "access_token").Value}", SeverityLevel.Information);

                            var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                            resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                            using var listsResponse = await _resiliencePipeline.ExecuteAsync(async rc =>
                            {
                                // TODO: $count=true cannot be used? Note: The $count and $search query parameters are currently not available in Azure AD B2C tenants.
                                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me/todo/lists?$top={Constants.Microsoft.Count}");
                                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _refreshTokenService.GetMicrosoftToken(user.Id, rc.CancellationToken));

                                return await _httpClient.SendAsync(request, cancellationToken);

                            }, resilienceContext);

                            ResilienceContextPool.Shared.Return(resilienceContext);

                            // TODO: ?$filter=status eq 'notStarted'

                            using var listsResponseJson = JsonDocument.Parse(await listsResponse.Content.ReadAsStringAsync(cancellationToken));

                            var listsJson = listsResponseJson.RootElement.GetProperty("value").EnumerateArray();

                            if (listsJson.Count() == Constants.Microsoft.Count)
                            {
                                throw new Exception("68344baf-45f0-4663-b5b1-8e0ae2c4c534");
                            }

                            var taskCount = 0;

                            foreach (var listJson in listsJson)
                            {
                                using var tasksResponse = await _resiliencePipeline.ExecuteAsync(async rc =>
                                {
                                    using var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me/todo/lists/{listJson.GetProperty("id").GetString()}/tasks?$filter=status eq 'notStarted'&$top={Constants.Microsoft.Count}");
                                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _refreshTokenService.GetMicrosoftToken(user.Id, rc.CancellationToken));

                                    return await _httpClient.SendAsync(request, cancellationToken);

                                }, resilienceContext);

                                ResilienceContextPool.Shared.Return(resilienceContext);

                                using var tasksResponseJson = JsonDocument.Parse(await tasksResponse.Content.ReadAsStringAsync(cancellationToken));

                                var tasksJson = tasksResponseJson.RootElement.GetProperty("value").EnumerateArray();

                                var tasksCount = tasksJson.Count();

                                if (tasksCount == Constants.Microsoft.Count)
                                {
                                    throw new Exception("68344baf-45f0-4663-b5b1-8e0ae2c4c534");
                                }

                                taskCount += tasksCount;
                            }

                            var userMetricProviderValue = await _dataContext.UserMetricProviderValues
                                .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Tasks" && umpv.ProviderName == "Microsoft" &&
                                                              umpv.MetricType == MetricType.Count && umpv.Time == date, cancellationToken: cancellationToken);

                            if (userMetricProviderValue == null)
                            {
                                _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                                {
                                    UserId = user.Id,
                                    MetricName = "Tasks",
                                    ProviderName = "Microsoft",
                                    MetricType = MetricType.Count,
                                    Time = date,
                                    Value = taskCount
                                });

                                await _dataContext.SaveChangesAsync(cancellationToken);
                            }
                            else if (userMetricProviderValue.Value != taskCount)
                            {
                                userMetricProviderValue.Value = taskCount;

                                _dataContext.Entry(userMetricProviderValue).State = EntityState.Modified;

                                await _dataContext.SaveChangesAsync(cancellationToken);
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
