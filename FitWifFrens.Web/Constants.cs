namespace FitWifFrens.Web
{
    public static class Constants
    {
        public const string LocalTimeZoneId = "AUS Eastern Standard Time";

        public static readonly int ProviderSearchDaysBack = 15;

        public static readonly TimeSpan EndOfPeriodDelay = TimeSpan.FromHours(3);

        public static class Microsoft
        {
            public const int Count = 1000;
        }

        public static class Withings
        {
            public static readonly int[] WebhookSubscriptions = [1, 4, 16];
        }
    }
}
