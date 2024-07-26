namespace FitWifFrens.Data
{
    public class UserMetricProvider
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string MetricName { get; set; }
        public string ProviderName { get; set; }
        public MetricProvider MetricProvider { get; set; }
    }
}
