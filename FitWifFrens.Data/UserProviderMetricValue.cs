namespace FitWifFrens.Data
{
    public class UserProviderMetricValue
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string ProviderName { get; set; }
        public string MetricName { get; set; }
        public MetricType MetricType { get; set; }
        public ProviderMetricValue ProviderMetricValue { get; set; }


        public DateTime Time { get; set; }

        public double Value { get; set; }
    }
}
