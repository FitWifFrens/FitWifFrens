namespace FitWifFrens.Data
{
    public class Deposit
    {
        public string Transaction { get; set; }

        public decimal Amount { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public DateTime Time { get; set; }
    }
}
