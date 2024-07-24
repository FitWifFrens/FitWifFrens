namespace FitWifFrens.Data
{
    public class ProviderMetricValue
    {
        public string ProviderName { get; set; }
        public Provider Provider { get; set; }

        public string MetricName { get; set; }
        public MetricType MetricType { get; set; }
        public MetricValue MetricValue { get; set; }

        public ICollection<UserProviderMetricValue> Users { get; set; }

        public ICollection<Goal> Goals { get; set; }
    }
}
