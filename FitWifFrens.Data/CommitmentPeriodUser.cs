namespace FitWifFrens.Data
{
    public class CommitmentPeriodUser
    {
        public Guid CommitmentId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public CommitmentPeriod Commitment { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public decimal Stake { get; set; }
        public decimal Reward { get; set; }
    }
}
