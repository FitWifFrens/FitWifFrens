namespace FitWifFrens.Data
{
    public class MetricProvider
    {
        public string MetricName { get; set; }
        public Metric Metric { get; set; }

        public string ProviderName { get; set; }
        public Provider Provider { get; set; }

        public ICollection<UserMetricProvider> Users { get; set; }
        public ICollection<UserMetricProviderValue> Values { get; set; }
        public ICollection<CommitmentPeriodUserGoal> Goals { get; set; }
    }
}
