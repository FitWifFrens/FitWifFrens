namespace FitWifFrens.Data
{
    public class UserMetricProviderValue
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string MetricName { get; set; }
        public string ProviderName { get; set; }
        public MetricProvider MetricProvider { get; set; }

        public MetricType MetricType { get; set; }
        public MetricValue MetricValue { get; set; }

        public DateTime Time { get; set; }

        public double Value { get; set; }
    }
}
