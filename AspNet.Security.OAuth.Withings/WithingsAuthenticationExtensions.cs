using AspNet.Security.OAuth.Withings;
using Microsoft.AspNetCore.Authentication;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class WithingsAuthenticationExtensions
    {
        public static AuthenticationBuilder AddWithings(this AuthenticationBuilder builder)
        {
            return builder.AddWithings(WithingsAuthenticationDefaults.AuthenticationScheme, options => { });
        }

        public static AuthenticationBuilder AddWithings(
            this AuthenticationBuilder builder,
            Action<WithingsAuthenticationOptions> configuration)
        {
            return builder.AddWithings(WithingsAuthenticationDefaults.AuthenticationScheme, configuration);
        }

        public static AuthenticationBuilder AddWithings(
            this AuthenticationBuilder builder,
            string scheme,
            Action<WithingsAuthenticationOptions> configuration)
        {
            return builder.AddWithings(scheme, WithingsAuthenticationDefaults.DisplayName, configuration);
        }

        public static AuthenticationBuilder AddWithings(
            this AuthenticationBuilder builder,
            string scheme,
            string caption,
            Action<WithingsAuthenticationOptions> configuration)
        {
            return builder.AddOAuth<WithingsAuthenticationOptions, WithingsAuthenticationHandler>(scheme, caption, configuration);
        }
    }
}
