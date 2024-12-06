using Quartz;

namespace FitWifFrens.Web.Background
{
    public class UpdateCommitmentPeriodUserGoalsJob : IJob
    {
        public static readonly JobKey JobKey = JobKey.Create(nameof(UpdateCommitmentPeriodUserGoalsJob));

        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
