namespace FitWifFrens.Data
{
    public class Commitment
    {
        public Guid Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }

        public int Amount { get; set; }

        public string ContractAddress { get; set; }

        public bool Complete { get; set; }

        public ICollection<CommitmentProvider> Providers { get; set; }

        public ICollection<CommittedUser> Users { get; set; }
    }
}
