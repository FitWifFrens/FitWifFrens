using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitWifFrens.Web.Background
{
    public class TelegramCorrelationSummaryService
    {
        private sealed record UserCorrelation(string Name, double AvgDietRating, double WeightChange);

        private readonly DataContext _dataContext;
        private readonly NotificationService _notificationService;
        private readonly AiSummaryService _aiSummaryService;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<TelegramCorrelationSummaryService> _logger;

        public TelegramCorrelationSummaryService(
            DataContext dataContext,
            NotificationService notificationService,
            AiSummaryService aiSummaryService,
            TimeProvider timeProvider,
            TelemetryClient telemetryClient,
            ILogger<TelegramCorrelationSummaryService> logger)
        {
            _dataContext = dataContext;
            _notificationService = notificationService;
            _aiSummaryService = aiSummaryService;
            _timeProvider = timeProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task SendWeeklyCorrelationSummary(CancellationToken cancellationToken)
        {
            try
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var monthStartTime = now.AddDays(-28);

                var pollData = await _dataContext.UserTelegramPollResponses
                    .AsNoTracking()
                    .Where(r => r.UserId != null && r.CommitmentPoll != null && r.AnsweredTime >= monthStartTime)
                    .GroupBy(r => new { UserId = r.UserId!, Nickname = r.User!.Nickname, UserName = r.User!.UserName })
                    .Select(g => new { g.Key.UserId, g.Key.Nickname, g.Key.UserName, AvgRating = g.Average(x => x.Value) })
                    .ToListAsync(cancellationToken);

                var weightData = await _dataContext.UserMetricProviderValues
                    .AsNoTracking()
                    .Where(v => v.MetricName == "Weight" && v.MetricType == MetricType.Value && v.Time >= monthStartTime)
                    .OrderBy(v => v.Time)
                    .Select(v => new { v.UserId, v.User.Nickname, v.User.UserName, v.Value })
                    .ToListAsync(cancellationToken);

                var weightByUser = weightData
                    .GroupBy(v => v.UserId)
                    .ToDictionary(g => g.Key, g => g.Last().Value - g.First().Value);

                var correlations = pollData
                    .Where(p => weightByUser.ContainsKey(p.UserId))
                    .Select(p =>
                    {
                        var name = !string.IsNullOrWhiteSpace(p.Nickname) ? p.Nickname!
                                   : !string.IsNullOrWhiteSpace(p.UserName) ? p.UserName!
                                   : p.UserId;
                        return new UserCorrelation(name, Math.Round(p.AvgRating, 2), Math.Round(weightByUser[p.UserId], 1));
                    })
                    .OrderBy(c => c.Name)
                    .ToList();

                if (correlations.Count == 0)
                {
                    return;
                }

                var userIds = pollData.Select(p => p.UserId).Distinct().ToList();
                var factsRaw = await _dataContext.UserFacts
                    .AsNoTracking()
                    .Where(f => f.UserId != null && userIds.Contains(f.UserId))
                    .Select(f => new { Name = f.User!.Nickname ?? f.User.UserName ?? f.UserId!, f.Fact })
                    .ToListAsync(cancellationToken);
                var userFacts = factsRaw
                    .GroupBy(f => f.Name)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Fact).ToList());

                var commentaryInputs = correlations.Select(c =>
                    (c.Name, c.AvgDietRating, c.WeightChange, GetFallbackCommentary(c.AvgDietRating, c.WeightChange)));

                var commentaries = await _aiSummaryService.GenerateCorrelationCommentaries(commentaryInputs, cancellationToken, userFacts);

                var message = BuildSummaryMessage(correlations, commentaries);
                await _notificationService.Notify(message);

                using var chartStream = BuildCorrelationChart(correlations);
                await _notificationService.NotifyWithPhoto(chartStream);
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                _logger.LogError(exception, "Failed sending weekly correlation summary.");
                throw;
            }
        }

        private static string BuildSummaryMessage(IReadOnlyList<UserCorrelation> correlations, Dictionary<string, string> commentaries)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Diet vs Weight - Past 4 Weeks");
            builder.AppendLine();

            foreach (var c in correlations)
            {
                var weightText = c.WeightChange < 0
                    ? $"{Math.Abs(c.WeightChange):F1} kg lost"
                    : c.WeightChange > 0
                        ? $"{c.WeightChange:F1} kg gained"
                        : "no change";

                var commentary = commentaries.TryGetValue(c.Name, out var aiComment) ? aiComment : GetFallbackCommentary(c.AvgDietRating, c.WeightChange);

                builder.AppendLine($"- {c.Name}: avg diet rating {c.AvgDietRating:F1}/5, {weightText}");
                builder.AppendLine($"  {commentary}");
            }

            var message = builder.ToString().TrimEnd();
            return message.Length <= 4000 ? message : message[..4000];
        }

        private static string GetFallbackCommentary(double avgRating, double weightChange)
        {
            // Above 3 = expects weight loss, below 3 = expects weight gain, 3 = flat
            var expectsLoss = avgRating > 3.0;
            var expectsGain = avgRating < 3.0;
            var expectsFlat = Math.Abs(avgRating - 3.0) < 0.2;

            var lost = weightChange < -0.2;
            var gained = weightChange > 0.2;
            var flat = !lost && !gained;

            if (expectsFlat && flat)
            {
                return "Perfectly balanced, as all things should be.";
            }

            if (expectsFlat && lost)
            {
                return "Rated flat but still dropping weight? Humble legend.";
            }

            if (expectsFlat && gained)
            {
                return "Rated flat but gaining... those snacks are filing themselves under 'miscellaneous'.";
            }

            if (expectsLoss && lost)
            {
                return "The diet is dieting. Keep it up!";
            }

            if (expectsLoss && flat)
            {
                return "Claims to be eating clean but the scale isn't buying it.";
            }

            if (expectsLoss && gained)
            {
                return "Rated diet above average but gained weight? The audacity. The delusion. The vibes.";
            }

            if (expectsGain && gained)
            {
                return "At least they're honest about it.";
            }

            if (expectsGain && flat)
            {
                return "Rated the diet poorly but didn't gain? Built different or just lucky.";
            }

            // expectsGain && lost
            return "Says the diet is bad but still losing weight? Suspicious. Very suspicious.";
        }

        private static MemoryStream BuildCorrelationChart(IReadOnlyList<UserCorrelation> correlations)
        {
            var plot = new ScottPlot.Plot();

            plot.Title("Diet Rating vs Weight Change (Past 4 Weeks)");
            plot.XLabel("Avg Diet Rating");
            plot.YLabel("Weight Change (kg)");

            var xs = correlations.Select(c => c.AvgDietRating).ToArray();
            var ys = correlations.Select(c => c.WeightChange).ToArray();

            var scatter = plot.Add.ScatterPoints(xs, ys);
            scatter.MarkerSize = 10;

            foreach (var c in correlations)
            {
                var text = plot.Add.Text(c.Name, c.AvgDietRating, c.WeightChange);
                text.LabelFontSize = 12;
                text.OffsetX = 8;
                text.OffsetY = -4;
            }

            // Zero line for weight change
            var zeroLine = plot.Add.HorizontalLine(0);
            zeroLine.Color = ScottPlot.Colors.Gray;
            zeroLine.LineWidth = 1;
            zeroLine.LinePattern = ScottPlot.LinePattern.Dashed;

            // Vertical line at rating 3 (flat point)
            var flatLine = plot.Add.VerticalLine(3);
            flatLine.Color = ScottPlot.Colors.Gray;
            flatLine.LineWidth = 1;
            flatLine.LinePattern = ScottPlot.LinePattern.Dashed;

            // Quadrant labels
            var topLeft = plot.Add.Text("Gaining + Bad Diet", 1.2, 0);
            topLeft.LabelFontSize = 10;
            topLeft.LabelFontColor = ScottPlot.Colors.Red.WithAlpha(150);

            var bottomRight = plot.Add.Text("Losing + Good Diet", 3.2, 0);
            bottomRight.LabelFontSize = 10;
            bottomRight.LabelFontColor = ScottPlot.Colors.Green.WithAlpha(150);

            plot.Axes.Bottom.Min = 0.5;
            plot.Axes.Bottom.Max = 5.5;

            var bytes = plot.GetImageBytes(950, 500, ScottPlot.ImageFormat.Png);
            return new MemoryStream(bytes);
        }
    }
}
