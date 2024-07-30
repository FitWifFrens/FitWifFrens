namespace FitWifFrens.Web.Background
{
    public class RefreshTokenServiceConfiguration
    {
        public record RefreshTokenConfiguration(string TokenEndpoint, string ClientId, string ClientSecret);

        public RefreshTokenConfiguration Microsoft { get; set; }
        public RefreshTokenConfiguration Strava { get; set; }
        public RefreshTokenConfiguration Withings { get; set; }
    }
}
