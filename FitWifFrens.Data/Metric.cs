namespace FitWifFrens.Data
{
    public class Metric
    {
        public string Name { get; set; }

        public ICollection<UserMetricProvider> Users { get; set; }

        public ICollection<MetricProvider> Providers { get; set; }
        public ICollection<MetricValue> Values { get; set; }
    }
}
