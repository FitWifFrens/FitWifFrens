namespace FitWifFrens.Data
{
    public class CommitmentPeriodUserGoal
    {
        public Guid CommitmentId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string UserId { get; set; }
        public CommitmentPeriodUser User { get; set; }

        public string ProviderName { get; set; }
        public string MetricName { get; set; }
        public MetricType MetricType { get; set; }
        public Goal Goal { get; set; }

        public double? Value { get; set; } // TODO: Measure?
        public bool Success { get; set; }
    }
}
