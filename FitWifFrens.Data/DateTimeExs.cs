namespace FitWifFrens.Data
{
    public static class DateTimeExs
    {
        // Duplicate in DateTimeOffsetExs
        // See https://referencesource.microsoft.com/#mscorlib/system/datetimeoffset.cs,7c6f98bb552ffed1
        private const long TicksPerDay = TimeSpan.TicksPerDay;
        private const int DaysPerYear = 365;
        private const int DaysPer4Years = DaysPerYear * 4 + 1;
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;
        private const int DaysPer400Years = DaysPer100Years * 4 + 1;
        private const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear;
        private const int DaysTo10000 = DaysPer400Years * 25 - 366;
        private const long MinTicks = 0;
        private const long MaxTicks = DaysTo10000 * TicksPerDay - 1;
        private const long UnixEpochTicks = TimeSpan.TicksPerDay * DaysTo1970;
        private const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond;
        private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond;

        // Same format used in CommonPy
        public static readonly string MinutePrecisionFileNameFormatString = "yyyy-MM-dd_HH-mm";
        public static readonly string SecondPrecisionFileNameFormatString = MinutePrecisionFileNameFormatString + "-ss";
        public static readonly string MillisecondPrecisionFileNameFormatString = SecondPrecisionFileNameFormatString + "-ff";

        public static DateTime WithUniversalTime(this DateTime dateTime)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        public static long UnixTimeSecondsToTicks(long seconds)
        {
            const long minSeconds = MinTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;
            const long maxSeconds = MaxTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;

            if (seconds < minSeconds || seconds > maxSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, null);
            }

            return seconds * TimeSpan.TicksPerSecond + UnixEpochTicks;
        }

        public static long UnixTimeMillisecondsToTicks(long milliseconds)
        {
            const long minMilliseconds = MinTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;
            const long maxMilliseconds = MaxTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;

            if (milliseconds < minMilliseconds || milliseconds > maxMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, null);
            }

            return milliseconds * TimeSpan.TicksPerMillisecond + UnixEpochTicks;
        }

        public static DateTime FromUnixTimeSeconds(long seconds)
        {
            return new DateTime(UnixTimeSecondsToTicks(seconds));
        }

        public static DateTime FromUnixTimeSeconds(long seconds, DateTimeKind kind)
        {
            return new DateTime(UnixTimeSecondsToTicks(seconds), kind);
        }

        public static DateTime FromUnixTimeMilliseconds(long milliseconds)
        {
            return new DateTime(UnixTimeMillisecondsToTicks(milliseconds));
        }

        public static DateTime FromUnixTimeMilliseconds(long milliseconds, DateTimeKind kind)
        {
            return new DateTime(UnixTimeMillisecondsToTicks(milliseconds), kind);
        }

        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            var seconds = dateTime.Ticks / TimeSpan.TicksPerSecond;
            return seconds - UnixEpochSeconds;
        }

        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            var milliseconds = dateTime.Ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UnixEpochMilliseconds;
        }

        public static long ToUnixTimeTicks(this DateTime dateTime)
        {
            var ticks = dateTime.Ticks;
            return ticks - UnixEpochTicks;
        }

        public static DateTime Max(this DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        public static DateTime Min(this DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }

        public static bool AlmostEqual(this DateTime value1, DateTime value2, TimeSpan maxAbsoluteError)
        {
            if (value1 == value2)
            {
                return true;
            }

            if (Math.Abs((value1 - value2).TotalMilliseconds) > maxAbsoluteError.TotalMilliseconds)
            {
                return false;
            }

            return true;
        }

        public static DateTime RoundUp(this DateTime dateTime, TimeSpan timeSpan)
        {
            var modDateTimeTicks = dateTime.Ticks % timeSpan.Ticks;
            var delta = modDateTimeTicks != 0 ? timeSpan.Ticks - modDateTimeTicks : 0;
            return new DateTime(dateTime.Ticks + delta, dateTime.Kind);
        }

        public static DateTime RoundDown(this DateTime dateTime, TimeSpan timeSpan)
        {
            var delta = dateTime.Ticks % timeSpan.Ticks;
            return new DateTime(dateTime.Ticks - delta, dateTime.Kind);
        }

        public static DateTime RoundToNearest(this DateTime dateTime, TimeSpan timeSpan)
        {
            var delta = dateTime.Ticks % timeSpan.Ticks;
            var roundUp = delta > timeSpan.Ticks / 2;
            var offset = roundUp ? timeSpan.Ticks : 0;

            return new DateTime(dateTime.Ticks + offset - delta, dateTime.Kind);
        }

        public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek)
        {
            var difference = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
            return dateTime.AddDays(-1 * difference).Date;
        }

        public static DateTime StartOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 01, 00, 00, 00, dateTime.Kind);
        }

        public static DateTime StartOfYear(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, 01, 01, 00, 00, 00, dateTime.Kind);
        }

        public static string ToMinutePrecisionFileNameFormat(this DateTime dateTime)
        {
            return dateTime.ToString(MinutePrecisionFileNameFormatString);
        }

        public static string ToSecondPrecisionFileNameFormat(this DateTime dateTime)
        {
            return dateTime.ToString(SecondPrecisionFileNameFormatString);
        }

        public static string ToMillisecondPrecisionFileNameFormat(this DateTime dateTime)
        {
            return dateTime.ToString(MillisecondPrecisionFileNameFormatString);
        }

        public static DateTime SpecifyUtcKind(this DateTime unspecified)
        {
            return DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
        }
    }
}
