namespace FitWifFrens.Data
{
    public class ChatMessage
    {
        public long Id { get; set; }

        public string ChatId { get; set; } = string.Empty;
        public Chat Chat { get; set; } = null!;

        public long TelegramUserId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }
}
