using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitWifFrens.Web.Background
{
    public class TelegramWeightSummaryService
    {
        private sealed record WeightAggregate(string UserId, string? Nickname, string? UserName, int Count, double WeightChange);

        private readonly DataContext _dataContext;
        private readonly NotificationService _notificationService;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<TelegramWeightSummaryService> _logger;

        public TelegramWeightSummaryService(
            DataContext dataContext,
            NotificationService notificationService,
            TimeProvider timeProvider,
            TelemetryClient telemetryClient,
            ILogger<TelegramWeightSummaryService> logger)
        {
            _dataContext = dataContext;
            _notificationService = notificationService;
            _timeProvider = timeProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task SendWeeklyWeightSummary(CancellationToken cancellationToken)
        {
            try
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var weekStartTime = now.AddDays(-7);
                var monthStartTime = now.AddDays(-28);

                var weeklyRaw = await _dataContext.UserMetricProviderValues
                    .AsNoTracking()
                    .Where(v => v.MetricName == "Weight" && v.MetricType == MetricType.Value && v.Time >= weekStartTime)
                    .OrderBy(v => v.Time)
                    .Select(v => new { v.UserId, v.User.Nickname, v.User.UserName, v.Value })
                    .ToListAsync(cancellationToken);

                var monthlyRaw = await _dataContext.UserMetricProviderValues
                    .AsNoTracking()
                    .Where(v => v.MetricName == "Weight" && v.MetricType == MetricType.Value && v.Time >= monthStartTime)
                    .OrderBy(v => v.Time)
                    .Select(v => new { v.UserId, v.User.Nickname, v.User.UserName, v.Value })
                    .ToListAsync(cancellationToken);

                var allTimeRaw = await _dataContext.UserMetricProviderValues
                    .AsNoTracking()
                    .Where(v => v.MetricName == "Weight" && v.MetricType == MetricType.Value)
                    .OrderBy(v => v.Time)
                    .Select(v => new { v.UserId, v.User.Nickname, v.User.UserName, v.Value })
                    .ToListAsync(cancellationToken);

                var weekly = weeklyRaw
                    .GroupBy(v => new { v.UserId, v.Nickname, v.UserName })
                    .Select(g => new WeightAggregate(
                        g.Key.UserId,
                        g.Key.Nickname,
                        g.Key.UserName,
                        g.Count(),
                        g.Last().Value - g.First().Value))
                    .ToList();

                var monthly = monthlyRaw
                    .GroupBy(v => new { v.UserId, v.Nickname, v.UserName })
                    .Select(g => new WeightAggregate(
                        g.Key.UserId,
                        g.Key.Nickname,
                        g.Key.UserName,
                        g.Count(),
                        g.Last().Value - g.First().Value))
                    .ToList();

                var allTime = allTimeRaw
                    .GroupBy(v => new { v.UserId, v.Nickname, v.UserName })
                    .Select(g => new WeightAggregate(
                        g.Key.UserId,
                        g.Key.Nickname,
                        g.Key.UserName,
                        g.Count(),
                        g.Last().Value - g.First().Value))
                    .ToList();

                var message = BuildSummaryMessage(weekly, monthly, allTime);
                await _notificationService.Notify(message);
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                _logger.LogError(exception, "Failed sending weekly weight summary.");
                throw;
            }
        }

        private static string BuildSummaryMessage(
            IReadOnlyCollection<WeightAggregate> weekly,
            IReadOnlyCollection<WeightAggregate> monthly,
            IReadOnlyCollection<WeightAggregate> allTime)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Weight Summary");
            builder.AppendLine();

            builder.AppendLine("Past 7 days:");
            AppendAggregateSection(builder, weekly);
            builder.AppendLine();

            builder.AppendLine("Past 4 weeks:");
            AppendAggregateSection(builder, monthly);
            builder.AppendLine();

            builder.AppendLine("All time:");
            AppendAggregateSection(builder, allTime);

            var message = builder.ToString().TrimEnd();
            return message.Length <= 4000 ? message : message[..4000];
        }

        private static void AppendAggregateSection(StringBuilder builder, IReadOnlyCollection<WeightAggregate> aggregates)
        {
            if (!aggregates.Any())
            {
                builder.AppendLine("- No weigh ins.");
                return;
            }

            foreach (var aggregate in aggregates.OrderBy(a => a.WeightChange).ThenBy(ResolveName))
            {
                var name = ResolveName(aggregate);
                var change = aggregate.WeightChange;
                var changeText = change < 0
                    ? $"{Math.Abs(change):F1} kg lost"
                    : change > 0
                        ? $"{change:F1} kg gained"
                        : "no change";
                builder.AppendLine($"- {name}: {aggregate.Count} weigh ins, {changeText}");
            }
        }

        private static string ResolveName(WeightAggregate aggregate)
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
