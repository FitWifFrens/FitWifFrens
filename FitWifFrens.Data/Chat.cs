namespace FitWifFrens.Data
{
    public class Chat
    {
        public string ChatId { get; set; } = string.Empty;

        public string? Title { get; set; }

        public DateTime CreatedTime { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ICollection<BotSoul> SoulTraits { get; set; } = new List<BotSoul>();
        public BotMemory? Memory { get; set; }
        public ICollection<CommitmentTelegramPoll> Polls { get; set; } = new List<CommitmentTelegramPoll>();
    }
}
