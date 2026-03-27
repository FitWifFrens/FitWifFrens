using FitWifFrens.Data;
using FitWifFrens.Web.Telegram;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Web.Background
{
    public class TelegramPollJobService
    {
        private readonly DataContext _dataContext;
        private readonly TelegramPollService _telegramPollService;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<TelegramPollJobService> _logger;

        public TelegramPollJobService(
            DataContext dataContext,
            TelegramPollService telegramPollService,
            TimeProvider timeProvider,
            TelemetryClient telemetryClient,
            ILogger<TelegramPollJobService> logger)
        {
            _dataContext = dataContext;
            _telegramPollService = telegramPollService;
            _timeProvider = timeProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task SendDailyCommitmentTelegramPolls(CancellationToken cancellationToken)
        {
            try
            {
                var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime.ConvertTimeFromUtc());

                var commitments = await _dataContext.Commitments
                    .AsNoTracking()
                    .Include(c => c.TelegramPollRule).ThenInclude(r => r.Options)
                    .Where(c => c.TelegramPollRule != null)
                    .Where(c => c.Periods.Any(p => p.Status == CommitmentPeriodStatus.Current && p.StartDate <= today && today < p.EndDate))
                    .ToListAsync(cancellationToken);

                foreach (var commitment in commitments)
                {
                    var rule = commitment.TelegramPollRule!;
                    var options = rule.Options.OrderBy(o => o.Index).Select(o => o.Text).ToArray();

                    var result = await _telegramPollService.SendPollAsync(
                        rule.Question,
                        options,
                        allowsMultipleAnswers: rule.AllowsMultipleAnswers,
                        isAnonymous: rule.IsAnonymous,
                        commitmentId: commitment.Id,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation(
                        "Daily commitment poll sent. CommitmentId={CommitmentId}, PollId={PollId}, MessageId={MessageId}, ChatId={ChatId}",
                        commitment.Id,
                        result.PollId,
                        result.MessageId,
                        result.ChatId);
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }
    }
}
