using Hangfire;

using FitWifFrens.Web.Telegram;

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
            _recurringJobManager.AddOrUpdate<StravaService>(nameof(StravaService) + nameof(StravaService.UpdateWebhook), s => s.UpdateWebhook(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<WithingsService>(nameof(WithingsService) + nameof(WithingsService.UpdateWebhooks), s => s.UpdateWebhooks(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<TelegramBotService>(nameof(TelegramBotService) + nameof(TelegramBotService.UpdateWebhook), s => s.UpdateWebhook(cancellationToken), Cron.Never);

            _recurringJobManager.AddOrUpdate<MicrosoftService>(nameof(MicrosoftService) + nameof(MicrosoftService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<StravaService>(nameof(StravaService) + nameof(StravaService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<WithingsService>(nameof(WithingsService) + nameof(WithingsService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<TelegramBotService>(nameof(TelegramBotService) + nameof(TelegramBotService.PullUpdates), s => s.PullUpdates(cancellationToken), Cron.Never);

            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.CreateCommitmentPeriods), s => s.CreateCommitmentPeriods(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriodUserGoals), s => s.UpdateCommitmentPeriodUserGoals(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriods), s => s.UpdateCommitmentPeriods(cancellationToken), Cron.Never);
            
            _recurringJobManager.AddOrUpdate<TelegramBotService>(nameof(TelegramBotService) + nameof(TelegramBotService.ExtractAllChatMemoriesAsync), s => s.ExtractAllChatMemoriesAsync(cancellationToken), Cron.Never);
#else
            _recurringJobManager.AddOrUpdate<StravaService>(nameof(StravaService) + nameof(StravaService.UpdateWebhook), s => s.UpdateWebhook(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<WithingsService>(nameof(WithingsService) + nameof(WithingsService.UpdateWebhooks), s => s.UpdateWebhooks(cancellationToken), Cron.Never);
            _recurringJobManager.AddOrUpdate<TelegramBotService>(nameof(TelegramBotService) + nameof(TelegramBotService.UpdateWebhook), s => s.UpdateWebhook(cancellationToken), Cron.Never);

            _recurringJobManager.AddOrUpdate<MicrosoftService>(nameof(MicrosoftService) + nameof(MicrosoftService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Hourly());
            _recurringJobManager.AddOrUpdate<StravaService>(nameof(StravaService) + nameof(StravaService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Hourly());
            _recurringJobManager.AddOrUpdate<WithingsService>(nameof(WithingsService) + nameof(WithingsService.UpdateProviderMetricValues), s => s.UpdateProviderMetricValues(cancellationToken), Cron.Hourly());
            _recurringJobManager.AddOrUpdate<TelegramBotService>(nameof(TelegramBotService) + nameof(TelegramBotService.PullUpdates), s => s.PullUpdates(cancellationToken), Cron.MinuteInterval(5));

            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.CreateCommitmentPeriods), s => s.CreateCommitmentPeriods(cancellationToken), Cron.Hourly(5));
            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriodUserGoals), s => s.UpdateCommitmentPeriodUserGoals(cancellationToken), Cron.Hourly(10));
            _recurringJobManager.AddOrUpdate<CommitmentPeriodService>(nameof(CommitmentPeriodService) + nameof(CommitmentPeriodService.UpdateCommitmentPeriods), s => s.UpdateCommitmentPeriods(cancellationToken), Cron.Hourly(15));
            
            _recurringJobManager.AddOrUpdate<TelegramBotService>(nameof(TelegramBotService) + nameof(TelegramBotService.ExtractAllChatMemoriesAsync), s => s.ExtractAllChatMemoriesAsync(cancellationToken), Cron.Daily(18));
#endif
            
            _recurringJobManager.AddOrUpdate<TelegramPollJobService>(
                nameof(TelegramPollJobService) + nameof(TelegramPollJobService.SendDailyCommitmentTelegramPolls),
                s => s.SendDailyCommitmentTelegramPolls(cancellationToken),
                Cron.Daily(9),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });
            
            _recurringJobManager.AddOrUpdate<TelegramPollSummaryService>(
                nameof(TelegramPollSummaryService) + nameof(TelegramPollSummaryService.SendWeeklyTelegramPollSummary),
                s => s.SendWeeklyTelegramPollSummary(cancellationToken),
                Cron.Weekly(DayOfWeek.Monday, 12, 0),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _recurringJobManager.AddOrUpdate<TelegramWeightSummaryService>(
                nameof(TelegramWeightSummaryService) + nameof(TelegramWeightSummaryService.SendWeeklyWeightSummary),
                s => s.SendWeeklyWeightSummary(cancellationToken),
                Cron.Weekly(DayOfWeek.Monday, 12, 1),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _recurringJobManager.AddOrUpdate<TelegramCorrelationSummaryService>(
                nameof(TelegramCorrelationSummaryService) + nameof(TelegramCorrelationSummaryService.SendWeeklyCorrelationSummary),
                s => s.SendWeeklyCorrelationSummary(cancellationToken),
                Cron.Weekly(DayOfWeek.Monday, 12, 2),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _recurringJobManager.AddOrUpdate<WeighInReminderService>(
                nameof(WeighInReminderService) + nameof(WeighInReminderService.SendWeighInReminders),
                s => s.SendWeighInReminders(cancellationToken),
                Cron.Daily(23),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });
            
            _backgroundJobClient.Enqueue<TelegramBotService>(s => s.RegisterBotCommandsAsync(CancellationToken.None));

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
