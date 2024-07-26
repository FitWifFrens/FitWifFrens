namespace FitWifFrens.Data
{
    public class MetricValue
    {
        public string MetricName { get; set; }
        public Metric Metric { get; set; }

        public MetricType Type { get; set; }

        public ICollection<Goal> Goals { get; set; }
        public ICollection<UserMetricProviderValue> Values { get; set; }
    }
}
