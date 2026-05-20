using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitWifFrens.Web.Background
{
    /// <summary>
    /// Builds a snapshot of the group's current state and lets the AI decide whether to post a
    /// spontaneous, non-event-tied message to the Telegram chat. Intended to be triggered manually
    /// from the Hangfire dashboard while we experiment with a recurring schedule.
    /// </summary>
    public class AmbientChatService
    {
        private const int RecentChatMessageCount = 20;
        private const int RecentWeighInDays = 7;
        private const int RecentActivityDays = 7;
        private const int WeightTrendDays = 28;

        private readonly DataContext _dataContext;
        private readonly NotificationService _notificationService;
        private readonly AiSummaryService _aiSummaryService;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<AmbientChatService> _logger;

        public AmbientChatService(
            DataContext dataContext,
            NotificationService notificationService,
            AiSummaryService aiSummaryService,
            TimeProvider timeProvider,
            TelemetryClient telemetryClient,
            ILogger<AmbientChatService> logger)
        {
            _dataContext = dataContext;
            _notificationService = notificationService;
            _aiSummaryService = aiSummaryService;
            _timeProvider = timeProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task SendAmbientMessage(CancellationToken cancellationToken)
        {
            try
            {
                var chatId = _notificationService.ChatId;
                var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
                var nowLocal = nowUtc.ConvertTimeFromUtc();

                var snapshot = await BuildSnapshot(chatId, nowUtc, nowLocal, cancellationToken);

                var userFacts = await LoadUserFacts(cancellationToken);
                var soulPrompt = await AiSummaryService.LoadSoulPromptAsync(_dataContext, chatId, cancellationToken);
                var memorySummary = await AiSummaryService.LoadMemorySummaryAsync(_dataContext, chatId, cancellationToken);

                var message = await _aiSummaryService.GenerateAmbientMessage(
                    snapshot,
                    cancellationToken,
                    userFacts.Count > 0 ? userFacts : null,
                    soulPrompt,
                    memorySummary);

                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogInformation("AmbientChatService: AI elected to stay silent.");
                    return;
                }

                _logger.LogInformation("AmbientChatService: posting ambient message ({Length} chars).", message.Length);
                await _notificationService.Notify(message);
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                _logger.LogError(exception, "Failed sending ambient chat message.");
                throw;
            }
        }

        private async Task<string> BuildSnapshot(string chatId, DateTime nowUtc, DateTime nowLocal, CancellationToken cancellationToken)
        {
            var weighInCutoff = nowUtc.AddDays(-RecentWeighInDays);
            var activityCutoff = nowUtc.AddDays(-RecentActivityDays);
            var weightTrendCutoff = nowUtc.AddDays(-WeightTrendDays);

            var users = await _dataContext.Users
                .AsNoTracking()
                .Where(u => u.TelegramUserId != null && !string.IsNullOrWhiteSpace(u.Nickname))
                .Select(u => new { u.Id, u.Nickname, u.UserName })
                .ToListAsync(cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine($"Local time: {nowLocal:dddd, yyyy-MM-dd HH:mm} ({Constants.LocalTimeZoneId})");
            sb.AppendLine($"UTC time:   {nowUtc:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            // Active commitment periods (current window)
            var today = DateOnly.FromDateTime(nowLocal);
            var activePeriods = await _dataContext.CommitmentPeriods
                .AsNoTracking()
                .Where(p => p.Status == CommitmentPeriodStatus.Current && p.StartDate <= today && p.EndDate >= today)
                .Select(p => new
                {
                    p.Commitment.Title,
                    p.StartDate,
                    p.EndDate,
                    UserCount = p.Users.Count,
                    TotalStake = p.Users.Sum(u => u.Stake)
                })
                .ToListAsync(cancellationToken);

            sb.AppendLine("Active commitments:");
            if (activePeriods.Count == 0)
            {
                sb.AppendLine("- (none)");
            }
            else
            {
                foreach (var p in activePeriods)
                {
                    var daysLeft = p.EndDate.DayNumber - today.DayNumber;
                    sb.AppendLine($"- \"{p.Title}\" — period {p.StartDate:yyyy-MM-dd} → {p.EndDate:yyyy-MM-dd} ({daysLeft} day(s) left), {p.UserCount} participant(s), total stake {p.TotalStake:0.####}");
                }
            }
            sb.AppendLine();

            // Per-user fitness state
            sb.AppendLine("Members:");
            if (users.Count == 0)
            {
                sb.AppendLine("- (no Telegram-linked users)");
            }
            else
            {
                foreach (var user in users)
                {
                    var name = !string.IsNullOrWhiteSpace(user.Nickname) ? user.Nickname! : user.UserName ?? user.Id;

                    var lastWeighIn = await _dataContext.UserMetricProviderValues
                        .AsNoTracking()
                        .Where(v => v.UserId == user.Id && v.MetricName == "Weight" && v.MetricType == MetricType.Value)
                        .OrderByDescending(v => v.Time)
                        .Select(v => new { v.Time, v.Value })
                        .FirstOrDefaultAsync(cancellationToken);

                    var trendWeights = await _dataContext.UserMetricProviderValues
                        .AsNoTracking()
                        .Where(v => v.UserId == user.Id && v.MetricName == "Weight" && v.MetricType == MetricType.Value && v.Time >= weightTrendCutoff)
                        .OrderBy(v => v.Time)
                        .Select(v => v.Value)
                        .ToListAsync(cancellationToken);

                    var exerciseMinutes = await _dataContext.UserMetricProviderValues
                        .AsNoTracking()
                        .Where(v => v.UserId == user.Id && v.MetricName == "Exercise" && v.MetricType == MetricType.Minutes && v.Time >= activityCutoff)
                        .SumAsync(v => v.Value, cancellationToken);

                    var runningMinutes = await _dataContext.UserMetricProviderValues
                        .AsNoTracking()
                        .Where(v => v.UserId == user.Id && v.MetricName == "Running" && v.MetricType == MetricType.Minutes && v.Time >= activityCutoff)
                        .SumAsync(v => v.Value, cancellationToken);

                    var workoutMinutes = await _dataContext.UserMetricProviderValues
                        .AsNoTracking()
                        .Where(v => v.UserId == user.Id && v.MetricName == "Workout" && v.MetricType == MetricType.Minutes && v.Time >= activityCutoff)
                        .SumAsync(v => v.Value, cancellationToken);

                    var recentPolls = await _dataContext.UserTelegramPollResponses
                        .AsNoTracking()
                        .Where(r => r.UserId == user.Id && r.CommitmentPoll != null && r.AnsweredTime >= weighInCutoff)
                        .Select(r => r.Value)
                        .ToListAsync(cancellationToken);

                    sb.Append("- ").Append(name).Append(": ");

                    if (lastWeighIn != null)
                    {
                        var daysSince = (int)Math.Floor((nowUtc - lastWeighIn.Time).TotalDays);
                        sb.Append($"weight {lastWeighIn.Value:F1}kg ({daysSince}d ago)");

                        if (trendWeights.Count >= 2)
                        {
                            var trend = trendWeights[^1] - trendWeights[0];
                            var trendText = trend < 0 ? $"{Math.Abs(trend):F1}kg lost"
                                            : trend > 0 ? $"{trend:F1}kg gained"
                                            : "no change";
                            sb.Append($", {trendText} over {WeightTrendDays}d");
                        }
                    }
                    else
                    {
                        sb.Append("no weigh-ins on record");
                    }

                    sb.Append($"; activity {RecentActivityDays}d: ex {exerciseMinutes:F0}m / run {runningMinutes:F0}m / workout {workoutMinutes:F0}m");

                    if (recentPolls.Count > 0)
                    {
                        sb.Append($"; diet polls last {RecentWeighInDays}d: avg {recentPolls.Average():F1}/5 ({recentPolls.Count} response(s))");
                    }
                    else
                    {
                        sb.Append("; no recent poll responses");
                    }

                    sb.AppendLine();
                }
            }
            sb.AppendLine();

            // Recent chat messages so the AI can "read the room"
            var recentMessages = await _dataContext.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.Timestamp)
                .Take(RecentChatMessageCount)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { m.DisplayName, m.Text, m.Timestamp })
                .ToListAsync(cancellationToken);

            sb.AppendLine($"Recent chat messages (last {recentMessages.Count}):");
            if (recentMessages.Count == 0)
            {
                sb.AppendLine("- (chat is quiet)");
            }
            else
            {
                foreach (var m in recentMessages)
                {
                    var local = m.Timestamp.ConvertTimeFromUtc();
                    var who = m.DisplayName == "Bot" ? "[You earlier]" : m.DisplayName;
                    var text = m.Text.Length > 200 ? m.Text[..200] + "…" : m.Text;
                    sb.AppendLine($"- [{local:MM-dd HH:mm}] {who}: {text}");
                }
            }

            return sb.ToString();
        }

        private async Task<Dictionary<string, List<string>>> LoadUserFacts(CancellationToken cancellationToken)
        {
            var rows = await _dataContext.UserFacts
                .AsNoTracking()
                .Where(f => f.UserId != null && f.User!.TelegramUserId != null)
                .Select(f => new { Name = f.User!.Nickname ?? f.User.UserName ?? f.UserId!, f.Fact })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => r.Name)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Fact).ToList());
        }
    }
}
