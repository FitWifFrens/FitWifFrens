namespace FitWifFrens.Data
{
    public class CommittedUser
    {
        public Guid CommitmentId { get; set; }
        public Commitment Commitment { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public string Transaction { get; set; }

        public int DistributedAmount { get; set; }
    }
}
