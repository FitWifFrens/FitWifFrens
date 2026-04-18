using System.Text;
using FitWifFrens.Data;
using MathNet.Numerics;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Web.Background
{
    public class CommitmentPeriodService
    {
        private readonly DataContext _dataContext;
        private readonly NotificationService _notificationService;
        private readonly AiSummaryService _aiSummaryService;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<CommitmentPeriodService> _logger;

        public CommitmentPeriodService(DataContext dataContext, NotificationService notificationService, AiSummaryService aiSummaryService, TimeProvider timeProvider, TelemetryClient telemetryClient, ILogger<CommitmentPeriodService> logger)
        {
            _dataContext = dataContext;
            _notificationService = notificationService;
            _aiSummaryService = aiSummaryService;
            _timeProvider = timeProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task CreateCommitmentPeriods(CancellationToken cancellationToken)
        {
            try
            {
                var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime.ConvertTimeFromUtc());

                var commitments = await _dataContext.Commitments
                    .Include(c => c.Users).ThenInclude(cu => cu.User.MetricProviders)
                    .Include(c => c.Goals)
                    .ToListAsync(cancellationToken);

                foreach (var commitment in commitments)
                {
                    // TODO: THROW CancellationToken
                    var metricName = commitment.Goals.Select(g => g.MetricName).Distinct().ToList();

                    var currentCommitmentPeriod = await _dataContext.CommitmentPeriods
                        .Include(cp => cp.Users)
                        .Where(cp => cp.CommitmentId == commitment.Id && cp.Status == CommitmentPeriodStatus.Current)
                        .SingleOrDefaultAsync(cancellationToken);

                    // If there is no current commitment period, and it is past the start date
                    // Or if there is a current commitment period, and it is past the end date
                    if ((currentCommitmentPeriod == null && date >= commitment.StartDate) || (currentCommitmentPeriod != null && date >= currentCommitmentPeriod.EndDate))
                    {
                        CommitmentPeriod nextCommitmentPeriod;
                        if (currentCommitmentPeriod != null)
                        {
                            currentCommitmentPeriod.Status = CommitmentPeriodStatus.Completing;

                            _dataContext.Entry(currentCommitmentPeriod).State = EntityState.Modified;

                            nextCommitmentPeriod = new CommitmentPeriod
                            {
                                CommitmentId = commitment.Id,
                                StartDate = currentCommitmentPeriod.EndDate,
                                EndDate = currentCommitmentPeriod.EndDate.AddDays(commitment.Days),
                                Status = CommitmentPeriodStatus.Current
                            };
                        }
                        else
                        {
                            nextCommitmentPeriod = new CommitmentPeriod
                            {
                                CommitmentId = commitment.Id,
                                StartDate = commitment.StartDate,
                                EndDate = commitment.StartDate.AddDays(commitment.Days),
                                Status = CommitmentPeriodStatus.Current
                            };
                        }

                        _dataContext.CommitmentPeriods.Add(nextCommitmentPeriod);

                        foreach (var commitmentUser in commitment.Users.Where(u => metricName.All(mn => u.User.MetricProviders.Any(mp => mp.MetricName == mn))))
                        {
                            commitmentUser.User.Balance -= commitmentUser.Stake; // TODO: negative balance?

                            _dataContext.Entry(commitmentUser.User).State = EntityState.Modified;

                            _dataContext.CommitmentPeriodUsers.Add(new CommitmentPeriodUser
                            {
                                CommitmentId = nextCommitmentPeriod.CommitmentId,
                                StartDate = nextCommitmentPeriod.StartDate,
                                EndDate = nextCommitmentPeriod.EndDate,
                                UserId = commitmentUser.UserId,
                                Stake = commitmentUser.Stake,
                                Goals = commitment.Goals.Select(g => new CommitmentPeriodUserGoal
                                {
                                    CommitmentId = nextCommitmentPeriod.CommitmentId,
                                    StartDate = nextCommitmentPeriod.StartDate,
                                    EndDate = nextCommitmentPeriod.EndDate,
                                    UserId = commitmentUser.UserId,
                                    MetricName = g.MetricName,
                                    MetricType = g.MetricType,
                                    ProviderName = commitmentUser.User.MetricProviders.Single(mp => mp.MetricName == g.MetricName).ProviderName,
                                }).ToList()
                            });
                        }

                        await _dataContext.SaveChangesAsync(cancellationToken);
                    }
                    else if (currentCommitmentPeriod != null)
                    {
                        // HACK: for testing so users are added after a period starts
                        foreach (var commitmentUser in commitment.Users
                                     .Where(cu => currentCommitmentPeriod.Users.All(cpu => cpu.UserId != cu.UserId))
                                     .Where(u => metricName.All(mn => u.User.MetricProviders.Any(mp => mp.MetricName == mn))))
                        {
                            commitmentUser.User.Balance -= commitmentUser.Stake;

                            _dataContext.Entry(commitmentUser.User).State = EntityState.Modified;

                            _dataContext.CommitmentPeriodUsers.Add(new CommitmentPeriodUser
                            {
                                CommitmentId = currentCommitmentPeriod.CommitmentId,
                                StartDate = currentCommitmentPeriod.StartDate,
                                EndDate = currentCommitmentPeriod.EndDate,
                                UserId = commitmentUser.UserId,
                                Stake = commitmentUser.Stake,
                                Goals = commitment.Goals.Select(g => new CommitmentPeriodUserGoal
                                {
                                    CommitmentId = currentCommitmentPeriod.CommitmentId,
                                    StartDate = currentCommitmentPeriod.StartDate,
                                    EndDate = currentCommitmentPeriod.EndDate,
                                    UserId = commitmentUser.UserId,
                                    MetricName = g.MetricName,
                                    MetricType = g.MetricType,
                                    ProviderName = commitmentUser.User.MetricProviders.Single(mp => mp.MetricName == g.MetricName).ProviderName,
                                }).ToList()
                            });
                        }

                        await _dataContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }

        public async Task CreateCommitmentPeriods(string userId, CancellationToken cancellationToken)
        {

        }

        public async Task UpdateCommitmentPeriodUserGoals(CancellationToken cancellationToken)
        {
            try
            {
                var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime.ConvertTimeFromUtc());

                var commitmentPeriods = await _dataContext.CommitmentPeriods
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Users)
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Goals)
                    .Include(cp => cp.Commitment).ThenInclude(c => c.TelegramPollRule)
                    .Include(cp => cp.Users).ThenInclude(cpu => cpu.Goals)
                    .Where(cp => cp.StartDate <= date && cp.Status == CommitmentPeriodStatus.Current)
                    .ToListAsync(cancellationToken);

                foreach (var commitmentPeriod in commitmentPeriods)
                {
                    var startTime = commitmentPeriod.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).ConvertTimeToUtc();
                    var endTime = commitmentPeriod.EndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).ConvertTimeToUtc();

                    foreach (var commitmentPeriodUser in commitmentPeriod.Users)
                    {
                        foreach (var commitmentPeriodUserGoals in commitmentPeriodUser.Goals.GroupBy(g => (g.MetricName, g.ProviderName)))
                        {
                            var isTelegramPollGoals = commitmentPeriod.Commitment.TelegramPollRule != null &&
                                                      commitmentPeriodUserGoals.Key.MetricName == "Telegram Poll" &&
                                                      commitmentPeriodUserGoals.Key.ProviderName == "Telegram";

                            var valuesStartTime = commitmentPeriodUserGoals.Key.MetricName == "Weight"
                                ? startTime.AddDays(-commitmentPeriod.Commitment.Days)
                                : startTime;

                            var userMetricProviderValues = await _dataContext.UserMetricProviderValues
                                .Where(umpv => umpv.UserId == commitmentPeriodUser.UserId && umpv.MetricName == commitmentPeriodUserGoals.Key.MetricName &&
                                               umpv.ProviderName == commitmentPeriodUserGoals.Key.ProviderName && umpv.Time >= valuesStartTime && umpv.Time < endTime)
                                .ToListAsync(cancellationToken);

                            foreach (var commitmentPeriodUserGoal in commitmentPeriodUserGoals)
                            {
                                var value = default(double?);
                                if (commitmentPeriodUserGoal.MetricType == MetricType.Count)
                                {
                                    if (isTelegramPollGoals)
                                    {
                                        var responses = await _dataContext.UserTelegramPollResponses
                                            .Where(r => r.UserId == commitmentPeriodUser.UserId &&
                                                        r.CommitmentPoll != null &&
                                                        r.CommitmentPoll.CommitmentId == commitmentPeriod.CommitmentId &&
                                                        r.AnsweredTime >= startTime &&
                                                        r.AnsweredTime < endTime)
                                            .ToListAsync(cancellationToken);

                                        value = responses.Any() ? responses.Count : null;
                                    }
                                    else if (commitmentPeriodUserGoal.MetricName == "Blood Pressure")
                                    {
                                        var values = userMetricProviderValues.Where(upmv => upmv.MetricType == MetricType.Count).ToList();

                                        value = values.Any() ? Math.Round(values.Sum(upmv => upmv.Value), 0) : null;
                                    }
                                    else
                                    {
                                        var values = userMetricProviderValues.Where(upmv => upmv.MetricType == MetricType.Minutes).ToList();

                                        value = values.Any() ? values.Count : null;
                                    }
                                }
                                else if (commitmentPeriodUserGoal.MetricType == MetricType.Minutes)
                                {
                                    var values = userMetricProviderValues.Where(upmv => upmv.MetricType == MetricType.Minutes).ToList();

                                    value = values.Any() ? Math.Round(values.Sum(upmv => upmv.Value), 2) : null;
                                }
                                else if (commitmentPeriodUserGoal.MetricType == MetricType.Value)
                                {
                                    if (isTelegramPollGoals)
                                    {
                                        var responses = await _dataContext.UserTelegramPollResponses
                                            .Where(r => r.UserId == commitmentPeriodUser.UserId &&
                                                        r.CommitmentPoll != null &&
                                                        r.CommitmentPoll.CommitmentId == commitmentPeriod.CommitmentId &&
                                                        r.AnsweredTime >= startTime &&
                                                        r.AnsweredTime < endTime)
                                            .ToListAsync(cancellationToken);

                                        value = responses.Any() ? Math.Round(responses.Average(r => r.Value), 2) : null;
                                    }
                                    else if (userMetricProviderValues.Count >= 2)
                                    {
                                        // TODO: convert to local
                                        var userMetricProviderValueByDay = userMetricProviderValues.GroupBy(umpv => umpv.Time.ConvertTimeFromUtc().Date).OrderBy(g => g.Key).ToList();

                                        if (userMetricProviderValueByDay.Count >= 2)
                                        {
                                            var times = userMetricProviderValueByDay.Select(g => (double)g.Key.ToUnixTimeSeconds()).ToArray();
                                            var values = userMetricProviderValueByDay.Select(g => g.Average(umpv => umpv.Value)).ToArray();

                                            var (_, slope) = Fit.Line(times, values);

                                            value = Math.Round(slope * commitmentPeriod.Commitment.Days * TimeSpan.FromDays(1).TotalSeconds, 1);
                                        }
                                    }

                                    //var endUserProviderMetricValue = userMetricProviderValues.MaxBy(upmv => upmv.Time);

                                    //if (endUserProviderMetricValue != null)
                                    //{
                                    //    var startUserMetricProviderValue = await _dataContext.UserMetricProviderValues
                                    //        .Where(umpv => umpv.UserId == commitmentPeriodUser.UserId && umpv.MetricName == commitmentPeriodUserGoal.MetricName &&
                                    //                       umpv.ProviderName == commitmentPeriodUserGoal.ProviderName && umpv.MetricType == MetricType.Value && umpv.Time <= startTime)
                                    //        .OrderByDescending(upmv => upmv.Time)
                                    //        .FirstOrDefaultAsync(cancellationToken);

                                    //    if (startUserMetricProviderValue != null)
                                    //    {
                                    //        value = Math.Round(endUserProviderMetricValue.Value - startUserMetricProviderValue.Value, 1);
                                    //    }
                                    //}
                                }
                                else
                                {
                                    throw new ArgumentOutOfRangeException(nameof(commitmentPeriodUserGoal.MetricType), commitmentPeriodUserGoal.MetricType, "220d6ad7-0701-4118-92ed-94bff8f984fb");
                                }

                                if (commitmentPeriodUserGoal.Value != value)
                                {
                                    commitmentPeriodUserGoal.Value = value;

                                    _dataContext.Entry(commitmentPeriodUserGoal).State = EntityState.Modified;
                                }
                            }
                        }
                    }
                }

                await _dataContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }

        public async Task UpdateCommitmentPeriodUserGoals(string userId, CancellationToken cancellationToken)
        {

        }

        // TODO: only complete once we know we have all the data
        public async Task UpdateCommitmentPeriods(CancellationToken cancellationToken)
        {
            try
            {
                var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime.Subtract(Constants.EndOfPeriodDelay).ConvertTimeFromUtc());

                var commitmentPeriods = await _dataContext.CommitmentPeriods
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Users)
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Goals)
                    .Include(cp => cp.Users).ThenInclude(cpu => cpu.User)
                    .Include(cp => cp.Users).ThenInclude(cpu => cpu.Goals).ThenInclude(cpug => cpug.Goal)
                    .Where(cp => cp.EndDate <= date && cp.Status == CommitmentPeriodStatus.Completing)
                    .ToListAsync(cancellationToken);

                var pendingSummaries = new List<PendingPeriodSummary>();

                foreach (var commitmentPeriod in commitmentPeriods)
                {
                    var commitmentPeriodUsersByResult = commitmentPeriod.Users.GroupBy(cpu =>
                    {
                        return cpu.Goals.All(cpug =>
                        {
                            if (cpug.Value == null)
                            {
                                return false;
                            }

                            return cpug.Goal.Rule switch
                            {
                                GoalRule.LessThan => cpug.Value < cpug.Goal.Value,
                                GoalRule.LessThanOrEqualTo => cpug.Value <= cpug.Goal.Value,
                                GoalRule.GreaterThan => cpug.Value > cpug.Goal.Value,
                                GoalRule.GreaterThanOrEqualTo => cpug.Value >= cpug.Goal.Value,
                                _ => throw new ArgumentOutOfRangeException(nameof(cpug.Goal.Rule), cpug.Goal.Rule, "bb9e2074-1757-4d37-b714-e135336b4143")
                            };
                        });
                    }).ToDictionary(cpugr => cpugr.Key, cpugr => cpugr.ToList());

                    if (!commitmentPeriodUsersByResult.ContainsKey(false))
                    {
                        commitmentPeriodUsersByResult[false] = new List<CommitmentPeriodUser>();
                    }
                    if (!commitmentPeriodUsersByResult.ContainsKey(true))
                    {
                        commitmentPeriodUsersByResult[true] = new List<CommitmentPeriodUser>();
                    }

                    var winners = commitmentPeriodUsersByResult[true];
                    var losers = commitmentPeriodUsersByResult[false];
                    var rolloverFromCommitment = commitmentPeriod.Commitment.Balance;
                    var rewardPerWinner = 0m;
                    var rolledIntoCommitment = 0m;

                    if (losers.Any() || winners.Any())
                    {
                        var failedStake = losers.Sum(cpu => cpu.Stake);

                        if (winners.Any())
                        {
                            rewardPerWinner = (failedStake + commitmentPeriod.Commitment.Balance) / winners.Count;

                            foreach (var commitmentPeriodUser in winners)
                            {
                                commitmentPeriodUser.Reward = rewardPerWinner + commitmentPeriodUser.Stake;

                                _dataContext.Entry(commitmentPeriodUser).State = EntityState.Modified;

                                var user = await _dataContext.Users.SingleAsync(u => u.Id == commitmentPeriodUser.UserId, cancellationToken);

                                user.Balance += rewardPerWinner + commitmentPeriodUser.Stake;

                                _dataContext.Entry(user).State = EntityState.Modified;
                            }

                            if (commitmentPeriod.Commitment.Balance > 0)
                            {
                                commitmentPeriod.Commitment.Balance = 0;

                                _dataContext.Entry(commitmentPeriod.Commitment).State = EntityState.Modified;
                            }
                        }
                        else if (failedStake > 0)
                        {
                            rolledIntoCommitment = failedStake;
                            commitmentPeriod.Commitment.Balance += failedStake;

                            _dataContext.Entry(commitmentPeriod.Commitment).State = EntityState.Modified;
                        }
                    }

                    commitmentPeriod.Status = CommitmentPeriodStatus.Complete;

                    _dataContext.Entry(commitmentPeriod).State = EntityState.Modified;

                    if (winners.Any() || losers.Any())
                    {
                        pendingSummaries.Add(new PendingPeriodSummary(
                            commitmentPeriod,
                            winners.Select(w => (User: w.User, Stake: w.Stake, Reward: w.Reward)).ToList(),
                            losers.Select(l => (User: l.User, Stake: l.Stake)).ToList(),
                            rewardPerWinner,
                            rolloverFromCommitment,
                            rolledIntoCommitment));
                    }
                }

                await _dataContext.SaveChangesAsync(cancellationToken);

                if (pendingSummaries.Count > 0)
                {
                    var soulPrompt = await AiSummaryService.LoadSoulPromptAsync(_dataContext, _notificationService.ChatId, cancellationToken);
                    var memorySummary = await AiSummaryService.LoadMemorySummaryAsync(_dataContext, _notificationService.ChatId, cancellationToken);

                    foreach (var pending in pendingSummaries)
                    {
                        var userFacts = await LoadUserFactsAsync(
                            pending.Winners.Select(w => w.User).Concat(pending.Losers.Select(l => l.User)),
                            cancellationToken);

                        var aiSummary = await _aiSummaryService.GenerateCommitmentPeriodSummary(
                            pending.CommitmentPeriod.Commitment.Title,
                            pending.Winners.Select(w => (ResolveName(w.User), w.Stake, w.Reward)),
                            pending.Losers.Select(l => (ResolveName(l.User), l.Stake)),
                            cancellationToken,
                            userFacts,
                            soulPrompt,
                            memorySummary);

                        var message = BuildPeriodSummary(pending, aiSummary);
                        await _notificationService.Notify(message);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }

        private sealed record PendingPeriodSummary(
            CommitmentPeriod CommitmentPeriod,
            IReadOnlyList<(User User, decimal Stake, decimal Reward)> Winners,
            IReadOnlyList<(User User, decimal Stake)> Losers,
            decimal RewardPerWinner,
            decimal RolloverFromCommitment,
            decimal RolledIntoCommitment);

        private static string BuildPeriodSummary(PendingPeriodSummary pending, AiSummaryService.CommitmentPeriodAiSummary aiSummary)
        {
            var builder = new StringBuilder();
            var commitmentPeriod = pending.CommitmentPeriod;
            var winners = pending.Winners;
            var losers = pending.Losers;

            builder.AppendLine($"🏁 {commitmentPeriod.Commitment.Title} — period {commitmentPeriod.StartDate:yyyy-MM-dd} to {commitmentPeriod.EndDate:yyyy-MM-dd} complete");

            if (!string.IsNullOrWhiteSpace(aiSummary.Intro))
            {
                builder.AppendLine(aiSummary.Intro);
            }

            builder.AppendLine();

            var failedStake = losers.Sum(l => l.Stake);

            if (winners.Any())
            {
                builder.AppendLine($"✅ Winners ({winners.Count}):");
                foreach (var winner in winners.OrderByDescending(w => w.Reward))
                {
                    var name = ResolveName(winner.User);
                    builder.AppendLine($"• {name} — stake {FormatAmount(winner.Stake)} back + {FormatAmount(pending.RewardPerWinner)} reward = {FormatAmount(winner.Reward)}");
                    if (aiSummary.Commentaries.TryGetValue(name, out var quip))
                    {
                        builder.AppendLine($"   💬 {quip}");
                    }
                }
                builder.AppendLine();
            }

            if (losers.Any())
            {
                builder.AppendLine($"❌ Losers ({losers.Count}):");
                foreach (var loser in losers.OrderByDescending(l => l.Stake))
                {
                    var name = ResolveName(loser.User);
                    builder.AppendLine($"• {name} — forfeited {FormatAmount(loser.Stake)}");
                    if (aiSummary.Commentaries.TryGetValue(name, out var quip))
                    {
                        builder.AppendLine($"   💬 {quip}");
                    }
                }
                builder.AppendLine();
            }

            if (winners.Any())
            {
                var potParts = new List<string>();
                if (failedStake > 0) potParts.Add($"{FormatAmount(failedStake)} forfeited");
                if (pending.RolloverFromCommitment > 0) potParts.Add($"{FormatAmount(pending.RolloverFromCommitment)} rolled over");

                if (potParts.Any())
                {
                    builder.AppendLine($"💰 Pot: {string.Join(" + ", potParts)} split across {winners.Count} winner(s)");
                }
            }
            else if (pending.RolledIntoCommitment > 0)
            {
                builder.AppendLine($"💰 No winners — {FormatAmount(pending.RolledIntoCommitment)} rolled into the next period's pot");
            }

            var message = builder.ToString().TrimEnd();
            return message.Length <= 4000 ? message : message[..4000];
        }

        private async Task<Dictionary<string, List<string>>> LoadUserFactsAsync(IEnumerable<User> users, CancellationToken cancellationToken)
        {
            var userIds = users.Where(u => u != null).Select(u => u.Id).Distinct().ToList();
            if (userIds.Count == 0)
            {
                return new Dictionary<string, List<string>>();
            }

            var factsRaw = await _dataContext.UserFacts
                .AsNoTracking()
                .Where(f => f.UserId != null && userIds.Contains(f.UserId))
                .Select(f => new { Name = f.User!.Nickname ?? f.User.UserName ?? f.UserId!, f.Fact })
                .ToListAsync(cancellationToken);

            return factsRaw
                .GroupBy(f => f.Name)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Fact).ToList());
        }

        private static string ResolveName(User user) =>
            user?.Nickname ?? user?.UserName ?? user?.Id ?? "Unknown";

        private static string FormatAmount(decimal amount) => amount.ToString("0.####");
    }
}
