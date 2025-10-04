namespace FitWifFrens.Data
{
    public class Commitment
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }

        public ICollection<CommitmentUser> Users { get; set; }
        public ICollection<Goal> Goals { get; set; }
    }
}
