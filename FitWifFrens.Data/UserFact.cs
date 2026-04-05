namespace FitWifFrens.Data
{
    public class UserFact
    {
        public long Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public string Fact { get; set; } = string.Empty;

        public DateTime CreatedTime { get; set; }
    }
}
