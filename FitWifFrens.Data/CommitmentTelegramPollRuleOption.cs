namespace FitWifFrens.Data
{
    public class CommitmentTelegramPollRuleOption
    {
        public Guid CommitmentId { get; set; }
        public int Index { get; set; }
        public CommitmentTelegramPollRule Rule { get; set; }

        public string Text { get; set; }
        public double Value { get; set; }
    }
}
