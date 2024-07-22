namespace AspNet.Security.OAuth.Withings
{
    public static class WithingsAuthenticationDefaults
    {
        public const string AuthenticationScheme = "Withings";

        public static readonly string DisplayName = "Withings";

        public static readonly string Issuer = "Withings";

        public static readonly string CallbackPath = "/signin-withings";

        public static readonly string AuthorizationEndpoint = "https://account.withings.com/oauth2_user/authorize2";

        public static readonly string TokenEndpoint = "https://wbsapi.withings.net/v2/oauth2";

        //public static readonly string UserInformationEndpoint = "https://wbsapi.withings.net/v2/userinfo";
    }
}
