using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Web.Background
{
    public class RefreshTokenService
    {
        private readonly record struct ProviderNameUserId(string ProviderName, string UserId);
        private readonly record struct AccessRefreshToken(string AccessToken, string RefreshToken);

        private readonly RefreshTokenServiceConfiguration _configuration;
        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;
        private readonly TimeProvider _timeProvider;

        private readonly Dictionary<ProviderNameUserId, AccessRefreshToken> _accessTokenByProviderUserId;

        public RefreshTokenService(RefreshTokenServiceConfiguration configuration, DataContext dataContext, IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
        {
            _configuration = configuration;
            _dataContext = dataContext;
            _timeProvider = timeProvider;
            _httpClient = httpClientFactory.CreateClient();

            _accessTokenByProviderUserId = new Dictionary<ProviderNameUserId, AccessRefreshToken>();
        }

        public Task<string> GetStravaToken(string userId, CancellationToken cancellationToken)
        {
            return GetToken("Withings", userId, cancellationToken);
        }

        public Task<string> GetWithingsToken(string userId, CancellationToken cancellationToken)
        {
            return GetToken("Strava", userId, cancellationToken);
        }

        private async Task<string> GetToken(string providerName, string userId, CancellationToken cancellationToken)
        {
            var providerNameUserId = new ProviderNameUserId(providerName, userId);

            if (!_accessTokenByProviderUserId.TryGetValue(providerNameUserId, out var accessRefreshToken))
            {
                var tokens = await _dataContext.UserTokens.Where(t => t.UserId == userId && t.LoginProvider == providerName).ToListAsync(cancellationToken);

                accessRefreshToken = new AccessRefreshToken(tokens.Single(t => t.Name == "access_token").Value!, tokens.Single(t => t.Name == "refresh_token").Value!);

                _accessTokenByProviderUserId.Add(providerNameUserId, accessRefreshToken);
            }

            return accessRefreshToken.AccessToken;
        }

        public async Task RefreshStravaToken(string userId, CancellationToken cancellationToken)
        {
            var providerNameUserId = new ProviderNameUserId("Strava", userId);

            var accessRefreshToken = _accessTokenByProviderUserId[providerNameUserId];

            var tokenRequestParameters = new Dictionary<string, string>()
            {
                { "client_id", _configuration.Strava.ClientId },
                { "client_secret", _configuration.Strava.ClientSecret },
                { "refresh_token", accessRefreshToken.RefreshToken },
                { "grant_type", "refresh_token" },
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters!);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration.Strava.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var responseJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), default, cancellationToken);

            // TODO: "{\"status\":503,\"body\":{},\"error\":\"Invalid Params: invalid code\"}"
            var userTokens = await _dataContext.UserTokens.Where(t => t.UserId == userId && t.LoginProvider == "Strava").ToListAsync(cancellationToken);

            var userTokenAccess = userTokens.Single(t => t.Name == "access_token");
            var userTokenRefresh = userTokens.Single(t => t.Name == "refresh_token");
            var userTokenExpiresAt = userTokens.Single(t => t.Name == "expires_at");

            userTokenAccess.Value = responseJson.RootElement.GetProperty("access_token").GetString()!;
            userTokenRefresh.Value = responseJson.RootElement.GetProperty("refresh_token").GetString()!;
            userTokenExpiresAt.Value = (_timeProvider.GetUtcNow() + TimeSpan.FromSeconds(responseJson.RootElement.GetProperty("expires_in").GetInt32())).ToString("O", CultureInfo.InvariantCulture);

            _dataContext.Entry(userTokenAccess).State = EntityState.Modified;
            _dataContext.Entry(userTokenRefresh).State = EntityState.Modified;
            _dataContext.Entry(userTokenExpiresAt).State = EntityState.Modified;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _accessTokenByProviderUserId[providerNameUserId] = new AccessRefreshToken(userTokenAccess.Value, userTokenRefresh.Value);
        }

        public async Task RefreshWithingsToken(string userId, CancellationToken cancellationToken)
        {
            var providerNameUserId = new ProviderNameUserId("Withings", userId);

            var accessRefreshToken = _accessTokenByProviderUserId[providerNameUserId];

            var tokenRequestParameters = new Dictionary<string, string>
            {
                { "client_id", _configuration.Withings.ClientId },
                { "client_secret", _configuration.Withings.ClientSecret },
                { "refresh_token", accessRefreshToken.RefreshToken },
                { "grant_type", "refresh_token" },
                { "action", "requesttoken" },
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters!);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration.Withings.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var responseJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), default, cancellationToken);

            // TODO: "{\"status\":503,\"body\":{},\"error\":\"Invalid Params: invalid code\"}"
            var userTokens = await _dataContext.UserTokens.Where(t => t.UserId == userId && t.LoginProvider == "Withings").ToListAsync(cancellationToken);

            var userTokenAccess = userTokens.Single(t => t.Name == "access_token");
            var userTokenRefresh = userTokens.Single(t => t.Name == "refresh_token");
            var userTokenExpiresAt = userTokens.Single(t => t.Name == "expires_at");

            userTokenAccess.Value = responseJson.RootElement.GetProperty("body").GetProperty("access_token").GetString()!;
            userTokenRefresh.Value = responseJson.RootElement.GetProperty("body").GetProperty("refresh_token").GetString()!;
            userTokenExpiresAt.Value = (_timeProvider.GetUtcNow() + TimeSpan.FromSeconds(responseJson.RootElement.GetProperty("body").GetProperty("expires_in").GetInt32())).ToString("O", CultureInfo.InvariantCulture);

            _dataContext.Entry(userTokenAccess).State = EntityState.Modified;
            _dataContext.Entry(userTokenRefresh).State = EntityState.Modified;
            _dataContext.Entry(userTokenExpiresAt).State = EntityState.Modified;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _accessTokenByProviderUserId[providerNameUserId] = new AccessRefreshToken(userTokenAccess.Value, userTokenRefresh.Value);
        }
    }
}

