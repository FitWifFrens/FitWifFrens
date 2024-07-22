using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;

namespace AspNet.Security.OAuth.Withings
{
    public class WithingsAuthenticationOptions : OAuthOptions
    {
        public WithingsAuthenticationOptions()
        {
            ClaimsIssuer = WithingsAuthenticationDefaults.Issuer;
            CallbackPath = WithingsAuthenticationDefaults.CallbackPath;

            AuthorizationEndpoint = WithingsAuthenticationDefaults.AuthorizationEndpoint;
            TokenEndpoint = WithingsAuthenticationDefaults.TokenEndpoint;

            Scope.Add("user.metrics");

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "userid");
        }
    }
}
