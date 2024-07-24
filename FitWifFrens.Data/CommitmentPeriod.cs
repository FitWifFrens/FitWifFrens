namespace FitWifFrens.Data
{
    public class CommitmentPeriod
    {
        public Guid CommitmentId { get; set; }
        public Commitment Commitment { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public CommitmentPeriodStatus Status { get; set; }

        public ICollection<CommitmentPeriodUser> Users { get; set; }
    }
}
