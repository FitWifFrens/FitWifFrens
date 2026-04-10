namespace FitWifFrens.Data
{
    public class BotMemory
    {
        public string ChatId { get; set; } = string.Empty;
        public Chat Chat { get; set; } = null!;

        public string Summary { get; set; } = string.Empty;

        public DateTime CreatedTime { get; set; }

        public DateTime UpdatedTime { get; set; }
    }
}
