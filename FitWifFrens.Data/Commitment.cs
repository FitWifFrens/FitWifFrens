namespace FitWifFrens.Data
{
    public class Commitment
    {
        public Guid Id { get; set; }

        public string ContractAddress { get; set; }

        public ICollection<CommitmentProvider> Providers { get; set; }

        public ICollection<CommittedUser> Users { get; set; }
    }
}
