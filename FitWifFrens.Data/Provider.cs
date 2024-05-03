namespace FitWifFrens.Data
{
    public class Provider
    {
        public string Name { get; set; }

        public ICollection<UserLogin> Logins { get; set; }

        public ICollection<CommitmentProvider> Commitments { get; set; }
    }
}
