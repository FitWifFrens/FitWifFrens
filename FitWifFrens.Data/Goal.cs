namespace FitWifFrens.Data
{
    public class Goal
    {
        public Guid CommitmentId { get; set; }
        public Commitment Commitment { get; set; }

        public string MetricName { get; set; }
        public MetricType MetricType { get; set; }
        public MetricValue MetricValue { get; set; }

        public GoalRule Rule { get; set; }
        public double Value { get; set; }

        public ICollection<CommitmentPeriodUserGoal> Users { get; set; }
    }
}
