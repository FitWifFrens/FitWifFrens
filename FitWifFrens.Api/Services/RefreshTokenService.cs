using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Api.Services
{
    public class RefreshTokenService
    {
        private readonly record struct ProviderNameUserId(string ProviderName, string UserId);
        private readonly record struct AccessRefreshToken(string AccessToken, string RefreshToken);

        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;

        private readonly Dictionary<ProviderNameUserId, AccessRefreshToken> _accessTokenByProviderUserId;

        public RefreshTokenService(DataContext dataContext, IHttpClientFactory httpClientFactory)
        {
            _dataContext = dataContext;
            _httpClient = httpClientFactory.CreateClient();

            _accessTokenByProviderUserId = new Dictionary<ProviderNameUserId, AccessRefreshToken>();
        }

        public Task<string> GetStravaToken(string userId, CancellationToken cancellationToken)
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
    }
}
