namespace FitWifFrens.Data
{
    public class Activity
    {
        public DateTime StartTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public double Distance { get; set; }
        public TimeSpan Duration { get; set; }
        public int ActiveCalories { get; set; }
        public int Steps { get; set; }

        public User User { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;

    }
}
