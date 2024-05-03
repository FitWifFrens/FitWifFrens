namespace FitWifFrens.Data
{
    public class Provider
    {
        public string Name { get; set; }
        public UserLogin Login { get; set; }


        public ICollection<CommitmentProvider> Commitments { get; set; }
    }
}
