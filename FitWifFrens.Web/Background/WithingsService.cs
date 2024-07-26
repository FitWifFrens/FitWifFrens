﻿using FitWifFrens.Data;
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

        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<WithingsService> _logger;

        private readonly ResiliencePipeline<ResponseJsonDocument> _resiliencePipeline;

        public WithingsService(DataContext dataContext, IHttpClientFactory httpClientFactory, RefreshTokenService refreshTokenService, TelemetryClient telemetryClient, ILogger<WithingsService> logger)
        {
            _dataContext = dataContext;
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
                .AddRetry(new RetryStrategyOptions<ResponseJsonDocument>
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<ResponseJsonDocument>().HandleResult(rd => !rd.Response.IsSuccessStatusCode)
                })
                .AddTimeout(TimeSpan.FromSeconds(10))
                .Build();
        }

        public async Task UpdateProviderMetricValues(CancellationToken cancellationToken)
        {
            try
            {
                var providerMetricValues = await _dataContext.ProviderMetricValues.Where(pmv => pmv.ProviderName == "Withings").ToListAsync(cancellationToken);

                if (providerMetricValues.Any())
                {
                    foreach (var user in await _dataContext.Users.Include(u => u.Tokens).ToListAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var tokens = user.Tokens.Where(t => t.LoginProvider == "Withings").ToList();

                        if (tokens.Any())
                        {
                            _telemetryClient.TrackTrace($"Updating Withings data for user {user.Id} with token {tokens.Single(t => t.Name == "access_token").Value}", SeverityLevel.Information);

                            var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                            resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                            // TODO: "{\"status\":401,\"body\":{},\"error\":\"XRequestID: Not provided invalid_token: The access token provided is invalid\"}"
                            using var responseJsonDocument = await _resiliencePipeline.ExecuteAsync(async rc =>
                            {
                                using var request = new HttpRequestMessage(HttpMethod.Post, "https://wbsapi.withings.net/measure");
                                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                                {
                                    { "action", "getmeas" },
                                    { "meastypes", "1" },
                                    { "startdate", DateTime.UtcNow.AddDays(-30).ToUnixTimeSeconds().ToString() },
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
                                        var measureValue = Math.Round(measureJson.GetProperty("value").GetInt32() / 1000.0, 1);

                                        var userProviderMetricValue = await _dataContext.UserProviderMetricValues
                                            .SingleOrDefaultAsync(upmv => upmv.UserId == user.Id && upmv.ProviderName == "Withings" && upmv.MetricName == "Weight" &&
                                                                          upmv.MetricType == MetricType.Value && upmv.Time == measureGroupTime, cancellationToken: cancellationToken);

                                        if (userProviderMetricValue == null)
                                        {
                                            _dataContext.UserProviderMetricValues.Add(new UserProviderMetricValue
                                            {
                                                UserId = user.Id,
                                                ProviderName = "Withings",
                                                MetricName = "Weight",
                                                MetricType = MetricType.Value,
                                                Time = measureGroupTime,
                                                Value = measureValue
                                            });

                                            await _dataContext.SaveChangesAsync(cancellationToken);
                                        }
                                        else if (userProviderMetricValue.Value != measureValue)
                                        {
                                            userProviderMetricValue.Value = measureValue;

                                            _dataContext.Entry(userProviderMetricValue).State = EntityState.Modified;

                                            await _dataContext.SaveChangesAsync(cancellationToken);
                                        }
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
