using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitWifFrens.Web.Background
{
    public class TelegramWeightSummaryService
    {
        private sealed record WeightAggregate(string UserId, string? Nickname, string? UserName, int Count, double WeightChange);
        private sealed record WeightDataPoint(DateTime Time, double Change);
        private sealed record UserWeightSeries(string Name, IReadOnlyList<WeightDataPoint> DataPoints);

        private readonly DataContext _dataContext;
        private readonly NotificationService _notificationService;
        private readonly AiSummaryService _aiSummaryService;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<TelegramWeightSummaryService> _logger;

        public TelegramWeightSummaryService(
            DataContext dataContext,
            NotificationService notificationService,
            AiSummaryService aiSummaryService,
            TimeProvider timeProvider,
            TelemetryClient telemetryClient,
            ILogger<TelegramWeightSummaryService> logger)
        {
            _dataContext = dataContext;
            _notificationService = notificationService;
            _aiSummaryService = aiSummaryService;
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

                var userIds = weekly.Select(w => w.UserId).Distinct().ToList();
                var factsRaw = await _dataContext.UserFacts
                    .AsNoTracking()
                    .Where(f => f.UserId != null && userIds.Contains(f.UserId))
                    .Select(f => new { Name = f.User!.Nickname ?? f.User.UserName ?? f.UserId!, f.Fact })
                    .ToListAsync(cancellationToken);
                var userFacts = factsRaw
                    .GroupBy(f => f.Name)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Fact).ToList());

                var soulPrompt = await AiSummaryService.LoadSoulPromptAsync(_dataContext, _notificationService.ChatId, cancellationToken);
                var memorySummary = await AiSummaryService.LoadMemorySummaryAsync(_dataContext, _notificationService.ChatId, cancellationToken);
                var introLine = await _aiSummaryService.GenerateWeightSummaryIntro(
                    weekly.Select(a => (ResolveName(a), a.WeightChange)),
                    cancellationToken,
                    userFacts,
                    soulPrompt,
                    memorySummary);

                var message = BuildSummaryMessage(introLine, weekly, monthly, allTime);
                await _notificationService.Notify(message);

                var threeMonthStartTime = now.AddMonths(-3);

                var threeMonthRaw = await _dataContext.UserMetricProviderValues
                    .AsNoTracking()
                    .Where(v => v.MetricName == "Weight" && v.MetricType == MetricType.Value && v.Time >= threeMonthStartTime)
                    .OrderBy(v => v.Time)
                    .Select(v => new { v.UserId, v.User.Nickname, v.User.UserName, v.Time, v.Value })
                    .ToListAsync(cancellationToken);

                var threeMonthSeries = threeMonthRaw
                    .GroupBy(v => new { v.UserId, v.Nickname, v.UserName })
                    .Select(g =>
                    {
                        var baseline = g.First().Value;
                        var name = !string.IsNullOrWhiteSpace(g.Key.Nickname) ? g.Key.Nickname!
                                   : !string.IsNullOrWhiteSpace(g.Key.UserName) ? g.Key.UserName!
                                   : g.Key.UserId;
                        var dataPoints = g.Select(v => new WeightDataPoint(v.Time, v.Value - baseline)).ToList();
                        return new UserWeightSeries(name, dataPoints);
                    })
                    .Where(s => s.DataPoints.Count >= 1)
                    .ToList();

                if (threeMonthSeries.Count > 0)
                {
                    using var chartStream = BuildThreeMonthChart(threeMonthSeries, threeMonthStartTime, now);
                    await _notificationService.NotifyWithPhoto(chartStream);
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                _logger.LogError(exception, "Failed sending weekly weight summary.");
                throw;
            }
        }

        private static string BuildSummaryMessage(
            string introLine,
            IReadOnlyCollection<WeightAggregate> weekly,
            IReadOnlyCollection<WeightAggregate> monthly,
            IReadOnlyCollection<WeightAggregate> allTime)
        {
            var builder = new StringBuilder();

            builder.AppendLine(introLine);
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

        private static MemoryStream BuildThreeMonthChart(IReadOnlyList<UserWeightSeries> series, DateTime startTime, DateTime endTime)
        {
            var plot = new ScottPlot.Plot();

            plot.Title("3 Month Weight Change");
            plot.YLabel("Change (kg)");
            plot.Axes.DateTimeTicksBottom();

            foreach (var userSeries in series)
            {
                var xs = userSeries.DataPoints.Select(dp => dp.Time.ToOADate()).ToArray();
                var ys = userSeries.DataPoints.Select(dp => dp.Change).ToArray();

                var scatter = plot.Add.Scatter(xs, ys);
                scatter.LegendText = userSeries.Name;
                scatter.LineWidth = 2;
                scatter.MarkerSize = 6;
            }

            var zeroLine = plot.Add.HorizontalLine(0);
            zeroLine.Color = ScottPlot.Colors.Gray;
            zeroLine.LineWidth = 1;
            zeroLine.LinePattern = ScottPlot.LinePattern.Dashed;

            plot.Axes.Bottom.Min = startTime.ToOADate();
            plot.Axes.Bottom.Max = endTime.ToOADate();

            plot.ShowLegend();

            var bytes = plot.GetImageBytes(950, 500, ScottPlot.ImageFormat.Png);
            return new MemoryStream(bytes);
        }
    }
}
