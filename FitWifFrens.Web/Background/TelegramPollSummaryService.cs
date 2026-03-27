using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitWifFrens.Web.Background
{
    public class TelegramPollSummaryService
    {
        private sealed record PollAggregate(string UserId, string? Nickname, string? UserName, int Count, double AverageValue);

        private readonly DataContext _dataContext;
        private readonly NotificationService _notificationService;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<TelegramPollSummaryService> _logger;

        public TelegramPollSummaryService(
            DataContext dataContext,
            NotificationService notificationService,
            TimeProvider timeProvider,
            TelemetryClient telemetryClient,
            ILogger<TelegramPollSummaryService> logger)
        {
            _dataContext = dataContext;
            _notificationService = notificationService;
            _timeProvider = timeProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task SendWeeklyTelegramPollSummary(CancellationToken cancellationToken)
        {
            try
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var weekStartTime = now.AddDays(-7);

                var weekly = await _dataContext.UserTelegramPollResponses
                    .AsNoTracking()
                    .Where(r => r.UserId != null && r.CommitmentPoll != null && r.AnsweredTime >= weekStartTime)
                    .GroupBy(r => new { UserId = r.UserId!, Nickname = r.User!.Nickname, UserName = r.User!.UserName })
                    .Select(g => new PollAggregate(
                        g.Key.UserId,
                        g.Key.Nickname,
                        g.Key.UserName,
                        g.Count(),
                        g.Average(x => x.Value)))
                    .ToListAsync(cancellationToken);

                var allTime = await _dataContext.UserTelegramPollResponses
                    .AsNoTracking()
                    .Where(r => r.UserId != null && r.CommitmentPoll != null)
                    .GroupBy(r => new { UserId = r.UserId!, Nickname = r.User!.Nickname, UserName = r.User!.UserName })
                    .Select(g => new PollAggregate(
                        g.Key.UserId,
                        g.Key.Nickname,
                        g.Key.UserName,
                        g.Count(),
                        g.Average(x => x.Value)))
                    .ToListAsync(cancellationToken);

                var question = await _dataContext.UserTelegramPollResponses
                    .AsNoTracking()
                    .Where(r => r.CommitmentPoll != null)
                    .Select(r => r.CommitmentPoll!.Rule.Question)
                    .Distinct()
                    .OrderBy(q => q)
                    .FirstOrDefaultAsync(cancellationToken);

                var message = BuildSummaryMessage(weekly, allTime, question);
                await _notificationService.Notify(message);
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                _logger.LogError(exception, "Failed sending weekly Telegram poll summary.");
                throw;
            }
        }

        private static string BuildSummaryMessage(
            IReadOnlyCollection<PollAggregate> weekly,
            IReadOnlyCollection<PollAggregate> allTime,
            string? question)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Q: {question ?? "How do you rate your diet?"}");
            builder.AppendLine();

            builder.AppendLine("Past 7 days:");
            AppendAggregateSection(builder, weekly);
            builder.AppendLine();

            builder.AppendLine("All time:");
            AppendAggregateSection(builder, allTime);

            var message = builder.ToString().TrimEnd();
            return message.Length <= 4000 ? message : message[..4000];
        }

        private static void AppendAggregateSection(StringBuilder builder, IReadOnlyCollection<PollAggregate> aggregates)
        {
            if (!aggregates.Any())
            {
                builder.AppendLine("- No responses.");
                return;
            }

            foreach (var aggregate in aggregates.OrderByDescending(a => a.Count).ThenBy(ResolveName))
            {
                var name = ResolveName(aggregate);
                builder.AppendLine($"- {name}: {aggregate.Count} responses, avg {aggregate.AverageValue:F2}");
            }
        }

        private static string ResolveName(PollAggregate aggregate)
        {
            if (!string.IsNullOrWhiteSpace(aggregate.Nickname))
            {
                return aggregate.Nickname!;
            }

            if (!string.IsNullOrWhiteSpace(aggregate.UserName))
            {
                return aggregate.UserName!;
            }

            return aggregate.UserId;
        }
    }
}
