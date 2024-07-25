using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;
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

        private readonly Dictionary<ProviderNameUserId, AccessRefreshToken> _accessTokenByProviderUserId;

        public RefreshTokenService(RefreshTokenServiceConfiguration configuration, DataContext dataContext, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _dataContext = dataContext;
            _httpClient = httpClientFactory.CreateClient();

            _accessTokenByProviderUserId = new Dictionary<ProviderNameUserId, AccessRefreshToken>();
        }

        public async Task<string> RefreshStravaToken()
        {
            return "";
        }


        public async Task<string> GetWithingsToken(string userId, CancellationToken cancellationToken)
        {
            var providerNameUserId = new ProviderNameUserId("Withings", userId);

            if (!_accessTokenByProviderUserId.TryGetValue(providerNameUserId, out var accessRefreshToken))
            {
                var tokens = await _dataContext.UserTokens.Where(t => t.UserId == userId && t.LoginProvider == "Withings").ToListAsync(cancellationToken);

                accessRefreshToken = new AccessRefreshToken(tokens.Single(t => t.Name == "access_token").Value!, tokens.Single(t => t.Name == "refresh_token").Value!);

                _accessTokenByProviderUserId.Add(providerNameUserId, accessRefreshToken);
            }

            return accessRefreshToken.AccessToken;
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
            var tokens = await _dataContext.UserTokens.Where(t => t.UserId == userId && t.LoginProvider == "Withings").ToListAsync(cancellationToken);

            var userAccessToken = tokens.Single(t => t.Name == "access_token");
            var userRefreshToken = tokens.Single(t => t.Name == "refresh_token");

            userAccessToken.Value = responseJson.RootElement.GetProperty("body").GetProperty("access_token").GetString()!;
            userRefreshToken.Value = responseJson.RootElement.GetProperty("body").GetProperty("refresh_token").GetString()!;

            _dataContext.Entry(userAccessToken).State = EntityState.Modified;
            _dataContext.Entry(userRefreshToken).State = EntityState.Modified;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _accessTokenByProviderUserId[providerNameUserId] = new AccessRefreshToken(userAccessToken.Value, userRefreshToken.Value);
        }
    }
}

