namespace FitWifFrens.Web
{
    public static class TimeZoneUtils
    {
        public static readonly TimeZoneInfo LocalTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Constants.LocalTimeZoneId);

        public static DateTime ConvertTimeFromUtc(this DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, LocalTimeZone);
        }

        public static DateTime ConvertTimeToUtc(this DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, LocalTimeZone);
        }
    }
}
