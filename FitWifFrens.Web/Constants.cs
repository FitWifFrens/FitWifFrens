namespace FitWifFrens.Web
{
    public static class Constants
    {
        public const string LocalTimeZoneId = "AUS Eastern Standard Time";

        public static readonly int ProviderSearchDaysBack = 45;

        public static readonly TimeSpan EndOfPeriodDelay = TimeSpan.FromHours(3);

        public static class Microsoft
        {
            public const int Count = 1000;
        }

        public static class Withings
        {
            public static readonly int[] WebhookSubscriptions = [1, 4, 16];
        }

        public static class Memory
        {
            /// <summary>Max character length for the bot memory summary stored in the database.</summary>
            public const int MaxSummaryLength = 65536;

            /// <summary>How far back to load messages for memory extraction (hours).</summary>
            public const int ExtractionWindowHours = 48;

            /// <summary>How old messages can be before they are pruned (days).</summary>
            public const int MessageRetentionDays = 2;

            /// <summary>How many recent messages to include as context when the bot replies to a mention.</summary>
            public const int MentionContextMessageCount = 50;
        }
    }
}
