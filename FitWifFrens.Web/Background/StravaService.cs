using FitWifFrens.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Web.Background
{
    public class StravaService
    {
        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<StravaService> _logger;

        public StravaService(DataContext dataContext, IHttpClientFactory httpClientFactory, ILogger<StravaService> logger)
        {
            _dataContext = dataContext;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task UpdateProviderMetricValues(CancellationToken cancellationToken)
        {
            _logger.LogWarning("UpdateProviderMetricValues");

            var providerMetricValues = await _dataContext.ProviderMetricValues.Where(pmv => pmv.ProviderName == "Strava").ToListAsync(cancellationToken);

            if (providerMetricValues.Any())
            {
                foreach (var user in await _dataContext.Users.Include(u => u.Tokens).ToListAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var tokens = user.Tokens.Where(t => t.LoginProvider == "Strava").ToList();

                    if (tokens.Any())
                    {
                        var accessToken = tokens.Single(t => t.Name == "access_token");
                        //var refreshToken = tokens.Single(t => t.Name == "refresh_token");
                        ;
                        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("https://www.strava.com/api/v3/athlete/activities", new Dictionary<string, string?>
                        {
                            { "after", DateTime.UtcNow.AddDays(-30).ToUnixTimeSeconds().ToString() },
                            { "per_page", "200" }
                        }));
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);

                        using var response = await _httpClient.SendAsync(request, cancellationToken);

                        if (!response.IsSuccessStatusCode)
                        {
                            ;
                        }

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
                            else if (activityType == "Workout" || activityType == "Yoga")
                            {

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
    }
}
