﻿using FitWifFrens.Data;
using Hangfire;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Web.Background
{
    // TODO: need to handle delete
    public class StravaService
    {
        private readonly BackgroundConfiguration _backgroundConfiguration;
        private readonly RefreshTokenServiceConfiguration _refreshTokenServiceConfiguration;
        private readonly DataContext _dataContext;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly HttpClient _httpClient;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly NotificationService _notificationService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<StravaService> _logger;

        private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

        public StravaService(BackgroundConfiguration backgroundConfiguration, RefreshTokenServiceConfiguration refreshTokenServiceConfiguration,
            DataContext dataContext, IBackgroundJobClient backgroundJobClient, IHttpClientFactory httpClientFactory, RefreshTokenService refreshTokenService, NotificationService notificationService, TelemetryClient telemetryClient, ILogger<StravaService> logger)
        {
            _backgroundConfiguration = backgroundConfiguration;
            _refreshTokenServiceConfiguration = refreshTokenServiceConfiguration;
            _dataContext = dataContext;
            _backgroundJobClient = backgroundJobClient;
            _refreshTokenService = refreshTokenService;
            _notificationService = notificationService;
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
                            await _refreshTokenService.RefreshStravaToken(userId, args.Context.CancellationToken);
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

        public async Task UpdateWebhook(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_backgroundConfiguration.CallbackUrl))
            {
                try
                {
                    var webhookRequestParameters = new Dictionary<string, string>()
                    {
                        { "client_id", _refreshTokenServiceConfiguration.Strava.ClientId },
                        { "client_secret", _refreshTokenServiceConfiguration.Strava.ClientSecret },
                        { "callback_url", $"{_backgroundConfiguration.CallbackUrl}/api/webhooks/strava" },
                        { "verify_token", "6e1f46ab-7f07-4e77-80c3-b156a1d43358" }, // TODO:
                    };

                    var requestContent = new FormUrlEncodedContent(webhookRequestParameters!);

                    using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.strava.com/api/v3/push_subscriptions");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = requestContent;

                    var response = await _httpClient.SendAsync(request, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

                }
                catch (Exception exception)
                {
                    _telemetryClient.TrackException(exception);
                    throw;
                }
            }
        }

        public async Task UpdateProviderMetricValues(string stravaId, CancellationToken cancellationToken)
        {
            try
            {
                var metricProviders = await _dataContext.MetricProviders.Where(mp => mp.ProviderName == "Strava").ToListAsync(cancellationToken);

                if (metricProviders.Any())
                {
                    var userLogin = await _dataContext.UserLogins.Include(ul => ul.User.Tokens).SingleOrDefaultAsync(ul => ul.LoginProvider == "Strava" && ul.ProviderKey == stravaId, cancellationToken: cancellationToken);

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
                var metricProviders = await _dataContext.MetricProviders.Where(mp => mp.ProviderName == "Strava").ToListAsync(cancellationToken);

                if (metricProviders.Any())
                {
                    foreach (var user in await _dataContext.Users.Where(u => u.Logins.Any(l => l.LoginProvider == "Strava")).Include(u => u.Tokens).ToListAsync(cancellationToken))
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
            var tokens = user.Tokens.Where(t => t.LoginProvider == "Strava").ToList();

            if (tokens.Any())
            {
                _telemetryClient.TrackTrace($"Updating Strava data for user {user.Id} with token {tokens.Single(t => t.Name == "access_token").Value}", SeverityLevel.Information);

                var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                using var response = await _resiliencePipeline.ExecuteAsync(async rc =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("https://www.strava.com/api/v3/athlete/activities", new Dictionary<string, string?>
                    {
                        { "after", DateTime.UtcNow.AddDays(-Constants.ProviderSearchDaysBack).ToUnixTimeSeconds().ToString() },
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
                        var userMetricProviderValue = await _dataContext.UserMetricProviderValues
                            .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Running" && umpv.ProviderName == "Strava" &&
                                                          umpv.MetricType == MetricType.Minutes && umpv.Time == activityTime, cancellationToken: cancellationToken);

                        if (userMetricProviderValue == null)
                        {
                            _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                            {
                                UserId = user.Id,
                                MetricName = "Running",
                                ProviderName = "Strava",
                                MetricType = MetricType.Minutes,
                                Time = activityTime,
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
                    else if (activityType == "Ride" || activityType == "VirtualRide")
                    {

                    }
                    else if (activityType == "Workout" || activityType == "WeightTraining" || activityType == "Yoga")
                    {
                        var userMetricProviderValue = await _dataContext.UserMetricProviderValues
                            .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Workout" && umpv.ProviderName == "Strava" &&
                                                          umpv.MetricType == MetricType.Minutes && umpv.Time == activityTime, cancellationToken: cancellationToken);

                        if (userMetricProviderValue == null)
                        {
                            if (!string.IsNullOrWhiteSpace(user.Nickname))
                            {
                                _ = _notificationService.Notify($"{user.Nickname} just logged a workout");
                            }

                            _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                            {
                                UserId = user.Id,
                                MetricName = "Workout",
                                ProviderName = "Strava",
                                MetricType = MetricType.Minutes,
                                Time = activityTime,
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
                            .SingleOrDefaultAsync(umpv => umpv.UserId == user.Id && umpv.MetricName == "Exercise" && umpv.ProviderName == "Strava" &&
                                                          umpv.MetricType == MetricType.Minutes && umpv.Time == activityTime, cancellationToken: cancellationToken);

                        if (userMetricProviderValue == null)
                        {
                            _dataContext.UserMetricProviderValues.Add(new UserMetricProviderValue
                            {
                                UserId = user.Id,
                                MetricName = "Exercise",
                                ProviderName = "Strava",
                                MetricType = MetricType.Minutes,
                                Time = activityTime,
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
