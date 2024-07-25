using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AspNet.Security.OAuth.Withings
{
    public partial class WithingsAuthenticationHandler : OAuthHandler<WithingsAuthenticationOptions>
    {
        public WithingsAuthenticationHandler(
            IOptionsMonitor<WithingsAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            return base.HandleRemoteAuthenticateAsync();
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
        {
            var tokenRequestParameters = new Dictionary<string, string>()
            {
                { "client_id", Options.ClientId },
                { "redirect_uri", context.RedirectUri },
                { "client_secret", Options.ClientSecret },
                { "code", context.Code },
                { "grant_type", "authorization_code" },
                { "action", "requesttoken" },
            };

            // PKCE https://tools.ietf.org/html/rfc7636#section-4.5, see BuildChallengeUrl
            if (context.Properties.Items.TryGetValue(OAuthConstants.CodeVerifierKey, out var codeVerifier))
            {
                tokenRequestParameters.Add(OAuthConstants.CodeVerifierKey, codeVerifier!);
                context.Properties.Items.Remove(OAuthConstants.CodeVerifierKey);
            }

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters!);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            requestMessage.Version = Backchannel.DefaultRequestVersion;
            var response = await Backchannel.SendAsync(requestMessage, Context.RequestAborted);
            var body = await response.Content.ReadAsStringAsync(Context.RequestAborted);

            // TODO: "{\"status\":503,\"body\":{},\"error\":\"Invalid Params: invalid code\"}"
            return response.IsSuccessStatusCode switch
            {
                true => PrepareSuccessOAuthTokenResponse(response, body),
                false => throw new Exception(),
                //false => PrepareFailedOAuthTokenResponse(response, body)
            };
        }

        private static OAuthTokenResponse PrepareSuccessOAuthTokenResponse(HttpResponseMessage response, string body)
        {
            using var responseJson = JsonDocument.Parse(body);

            if (responseJson.RootElement.GetProperty("status").GetInt32() == 0)
            {
                return OAuthTokenResponse.Success(JsonDocument.Parse(responseJson.RootElement.GetProperty("body").GetRawText()));
            }
            else
            {
                throw new Exception();
            }
        }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            OAuthTokenResponse tokens)
        {
            var principal = new ClaimsPrincipal(identity);
            var context = new OAuthCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, tokens, tokens.Response!.RootElement);
            context.RunClaimActions();

            await Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
        }

        private static partial class Log
        {
            internal static async Task UserProfileErrorAsync(ILogger logger, HttpResponseMessage response, CancellationToken cancellationToken)
            {
                UserProfileError(
                    logger,
                    response.StatusCode,
                    response.Headers.ToString(),
                    await response.Content.ReadAsStringAsync(cancellationToken));
            }

            [LoggerMessage(1, LogLevel.Error, "An error occurred while retrieving the user profile: the remote server returned a {Status} response with the following payload: {Headers} {Body}.")]
            private static partial void UserProfileError(
                ILogger logger,
                System.Net.HttpStatusCode status,
                string headers,
                string body);
        }
    }
}
