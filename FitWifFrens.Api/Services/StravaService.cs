using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Api.Services
{
    public class StravaService
    {
        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<StravaService> _logger;
        public StravaService(DataContext dataContext, IHttpClientFactory httpClientFactory, IConfiguration configuration,
            TelemetryClient telemetryClient, ILogger<StravaService> logger)
        {
            _dataContext = dataContext;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task SaveTokensAsync(string userId, string loginProvider, string accessToken, string refreshToken, DateTime expiresAt)
        {
            var userToken = await _dataContext.UserTokens
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.LoginProvider == loginProvider);

            if (userToken == null)
            {
                userToken = new UserToken
                {
                    UserId = userId,
                    LoginProvider = loginProvider,
                    Name = "access_token",
                    Value = accessToken,
                };
                _dataContext.UserTokens.Add(userToken);

                var refreshUserToken = new UserToken
                {
                    UserId = userId,
                    LoginProvider = loginProvider,
                    Name = "refresh_token",
                    Value = refreshToken,
                };
                _dataContext.UserTokens.Add(refreshUserToken);
            }
            else
            {
                userToken.Value = accessToken;
                var refreshUserToken = await _dataContext.UserTokens
                    .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.LoginProvider == loginProvider && ut.Name == "refresh_token");

                if (refreshUserToken != null)
                {
                    refreshUserToken.Value = refreshToken;
                }
                else
                {
                    refreshUserToken = new UserToken
                    {
                        UserId = userId,
                        LoginProvider = loginProvider,
                        Name = "refresh_token",
                        Value = refreshToken,
                    };
                    _dataContext.UserTokens.Add(refreshUserToken);
                }
            }

            await _dataContext.SaveChangesAsync();
        }

        public async Task GetUserActivitiesAsync(string accessToken, CancellationToken cancellationToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.GetAsync("https://www.strava.com/api/v3/athlete/activities", cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var activities = JsonSerializer.Deserialize<string>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Strava activities");

            }
        }

        public async Task UpdateWebhook(CancellationToken cancellationToken)
        {
            var verifyToken = _configuration["Authentication:Strava:VerifyToken"];
            const string callbackUrl = "http://localhost:5235";

            try
            {
                var webhookRequestParameters = new Dictionary<string, string>
                    {
                        { "client_id", _configuration["Authentication:Strava:ClientId"] },
                        { "client_secret", _configuration["Authentication:Strava:ClientSecret"] },
                        { "callback_url", $"{callbackUrl}/api/webhooks/strava" },
                        { "verify_token", verifyToken }
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
}
