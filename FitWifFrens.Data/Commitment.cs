namespace FitWifFrens.Data
{
    public class Commitment
    {
        public Guid Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }

        public DateOnly StartDate { get; set; }
        public int Days { get; set; }

        public string ContractAddress { get; set; }

        public ICollection<Goal> Goals { get; set; }

        public ICollection<CommitmentPeriod> Periods { get; set; }

        public ICollection<CommitmentUser> Users { get; set; }
    }
}
