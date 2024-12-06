using Quartz;

namespace FitWifFrens.Web.Background
{
    public class StravaJob : IJob
    {
        public static readonly JobKey JobKey = JobKey.Create(nameof(StravaJob));

        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
