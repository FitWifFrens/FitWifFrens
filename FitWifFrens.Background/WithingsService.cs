using FitWifFrens.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Background
{
    public class WithingsService : IHostedService
    {
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WithingsService> _logger;

        public WithingsService(IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient, DataContext dataContext, IHttpClientFactory httpClientFactory, ILogger<WithingsService> logger)
        {
            _recurringJobManager = recurringJobManager;
            _backgroundJobClient = backgroundJobClient;
            _dataContext = dataContext;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //_recurringJobManager.AddOrUpdate(nameof(WithingsService), () => UpdateProviderMetricValues(cancellationToken), "*/1 * * * *"); // Cron.Hourly()

            _backgroundJobClient.Schedule(() => UpdateProviderMetricValues(cancellationToken), TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        public async Task UpdateProviderMetricValues(CancellationToken cancellationToken)
        {
            _logger.LogWarning("UpdateProviderMetricValues");

            var providerMetricValues = await _dataContext.ProviderMetricValues.Where(pmv => pmv.ProviderName == "Withings").ToListAsync(cancellationToken);

            if (providerMetricValues.Any())
            {
                foreach (var user in await _dataContext.Users.Include(u => u.Tokens).ToListAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var tokens = user.Tokens.Where(t => t.LoginProvider == "Withings").ToList();

                    if (tokens.Any())
                    {
                        var accessToken = tokens.Single(t => t.Name == "access_token");
                        //var refreshToken = tokens.Single(t => t.Name == "refresh_token");

                        using var request = new HttpRequestMessage(HttpMethod.Post, "https://wbsapi.withings.net/measure");
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "action", "getmeas" },
                            { "meastypes", "1" },
                            { "startdate", DateTime.UtcNow.AddDays(-30).ToUnixTimeSeconds().ToString() },
                        });
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);

                        using var response = await _httpClient.SendAsync(request, cancellationToken);

                        if (!response.IsSuccessStatusCode)
                        {
                            ;
                        }
                        // TODO: "{\"status\":401,\"body\":{},\"error\":\"XRequestID: Not provided invalid_token: The access token provided is invalid\"}"
                        using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

                        foreach (var measureGroupJson in responseJson.RootElement.GetProperty("body").GetProperty("measuregrps").EnumerateArray())
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

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _recurringJobManager.RemoveIfExists(nameof(WithingsService));

            return Task.CompletedTask;
        }
    }
}
