namespace FitWifFrens.Data
{
    public class UserTelegramPollResponse
    {
        public long Id { get; set; }

        public long UpdateId { get; set; }

        public string PollId { get; set; }
        public CommitmentTelegramPoll? CommitmentPoll { get; set; }

        public int OptionIndex { get; set; }
        public double Value { get; set; }

        public long TelegramUserId { get; set; }

        public string? UserId { get; set; }
        public User? User { get; set; }

        public DateTime AnsweredTime { get; set; }
    }
}
