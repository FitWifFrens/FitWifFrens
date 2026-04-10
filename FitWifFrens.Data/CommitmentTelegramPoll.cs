namespace FitWifFrens.Data
{
    public class CommitmentTelegramPoll
    {
        public string PollId { get; set; }

        public Guid CommitmentId { get; set; }
        public CommitmentTelegramPollRule Rule { get; set; }

        public int MessageId { get; set; }
        public string ChatId { get; set; }
        public Chat? Chat { get; set; }
        public DateTime SentTime { get; set; }

        public ICollection<UserTelegramPollResponse> Responses { get; set; }
    }
}
