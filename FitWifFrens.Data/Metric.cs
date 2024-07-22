namespace FitWifFrens.Data
{
    public class Metric
    {
        public string Name { get; set; }

        public ICollection<MetricValue> Values { get; set; }
    }
}
