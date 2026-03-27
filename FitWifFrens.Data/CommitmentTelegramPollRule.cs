namespace FitWifFrens.Data
{
    public class CommitmentTelegramPollRule
    {
        public Guid CommitmentId { get; set; }
        public Commitment Commitment { get; set; }

        public string Question { get; set; }
        public bool RequireDailyResponses { get; set; } = true;
        public bool AllowsMultipleAnswers { get; set; }
        public bool IsAnonymous { get; set; }

        public ICollection<CommitmentTelegramPollRuleOption> Options { get; set; }
        public ICollection<CommitmentTelegramPoll> Polls { get; set; }
    }
}
