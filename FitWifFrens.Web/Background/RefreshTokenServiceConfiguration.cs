namespace FitWifFrens.Web.Background
{
    public class RefreshTokenServiceConfiguration
    {
        public record RefreshTokenConfiguration(string TokenEndpoint, string ClientId, string ClientSecret);

        public RefreshTokenConfiguration Microsoft { get; init; }
        public RefreshTokenConfiguration Strava { get; init; }
        public RefreshTokenConfiguration Withings { get; init; }
    }
}
