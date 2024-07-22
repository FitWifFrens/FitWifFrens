namespace FitWifFrens.Data
{
    public class CommitmentUser
    {
        public Guid CommitmentId { get; set; }
        public Commitment Commitment { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public decimal Stake { get; set; }
    }
}
