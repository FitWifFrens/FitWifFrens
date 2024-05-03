namespace FitWifFrens.Data
{
    public class CommitmentProvider
    {
        public Guid CommitmentId { get; set; }
        public Commitment Commitment { get; set; }

        public string ProviderName { get; set; }
        public Provider Provider { get; set; }
    }
}
