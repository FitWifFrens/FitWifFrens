using Hangfire;

namespace FitWifFrens.Web.Background
{
    public class JobService : IHostedService
    {
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public JobService(IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient)
        {
            _recurringJobManager = recurringJobManager;
            _backgroundJobClient = backgroundJobClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
#if DEBUG
            _backgroundJobClient.Schedule<StravaService>(s => s.UpdateProviderMetricValues(cancellationToken), TimeSpan.FromSeconds(1));
            _backgroundJobClient.Schedule<WithingsService>(s => s.UpdateProviderMetricValues(cancellationToken), TimeSpan.FromSeconds(1));

            _backgroundJobClient.Schedule<CommitmentPeriodService>(s => s.CreateCommitmentPeriods(cancellationToken), TimeSpan.FromSeconds(15));
            _backgroundJobClient.Schedule<CommitmentPeriodService>(s => s.UpdateCommitmentPeriodUserGoals(cancellationToken), TimeSpan.FromSeconds(30));
            _backgroundJobClient.Schedule<CommitmentPeriodService>(s => s.UpdateCommitmentPeriods(cancellationToken), TimeSpan.FromSeconds(45));
#else
            _recurringJobManager.AddOrUpdate<StravaService>(nameof(StravaService) + nameof(StravaService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Hourly());
            _recurringJobManager.AddOrUpdate<WithingsService>(nameof(WithingsService) + nameof(WithingsService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Hourly());

            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.CreateCommitmentPeriods), s => s.CreateCommitmentPeriods(cancellationToken), Cron.Hourly(5));
            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriodUserGoals), s => s.UpdateCommitmentPeriodUserGoals(cancellationToken), Cron.Hourly(10));
            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriods), s => s.UpdateCommitmentPeriods(cancellationToken), Cron.Hourly(15));
#endif
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_recurringJobManager.RemoveIfExists(nameof(StravaService) + nameof(StravaService.UpdateProviderMetricValues));
            //_recurringJobManager.RemoveIfExists(nameof(WithingsService) + nameof(WithingsService.UpdateProviderMetricValues));

            //_recurringJobManager.RemoveIfExists(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.CreateCommitmentPeriods));
            //_recurringJobManager.RemoveIfExists(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriodUserGoals));
            //_recurringJobManager.RemoveIfExists(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriods));

            return Task.CompletedTask;
        }
    }
}
