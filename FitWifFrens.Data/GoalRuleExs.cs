namespace FitWifFrens.Data
{
    public static class GoalRuleExs
    {
        public static string ToDisplayString(this GoalRule goalRule)
        {
            return goalRule switch
            {
                GoalRule.LessThan => "<",
                GoalRule.LessThanOrEqualTo => "<=",
                GoalRule.GreaterThan => ">",
                GoalRule.GreaterThanOrEqualTo => ">=",
                _ => throw new ArgumentOutOfRangeException(nameof(goalRule), goalRule, "bf8e6383-abbf-4a35-8cf7-dad631c2c744")
            };
        }
    }
}
