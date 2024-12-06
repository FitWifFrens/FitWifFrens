using Quartz;

namespace FitWifFrens.Web.Background
{
    public class WithingsJob : IJob
    {
        public static readonly JobKey JobKey = JobKey.Create(nameof(WithingsJob));

        public Task Execute(IJobExecutionContext context)
        {
            return Task.CompletedTask;
        }
    }
}
