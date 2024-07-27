using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Web.Background
{
    public class CommitmentPeriodService
    {
        private readonly DataContext _dataContext;
        private readonly TimeProvider _timeProvider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<CommitmentPeriodService> _logger;

        public CommitmentPeriodService(DataContext dataContext, TimeProvider timeProvider, TelemetryClient telemetryClient, ILogger<CommitmentPeriodService> logger)
        {
            _dataContext = dataContext;
            _timeProvider = timeProvider;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task CreateCommitmentPeriods(CancellationToken cancellationToken)
        {
            try
            {
                var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

                var commitments = await _dataContext.Commitments
                    .Include(c => c.Users).ThenInclude(cu => cu.User.MetricProviders)
                    .Include(c => c.Goals)
                    .ToListAsync(cancellationToken);

                foreach (var commitment in commitments)
                {
                    // TODO: THROW CancellationToken
                    var metricName = commitment.Goals.Select(g => g.MetricName).Distinct().ToList();

                    var commitmentPeriod = await _dataContext.CommitmentPeriods
                        .Include(cp => cp.Users)
                        .Where(cp => cp.CommitmentId == commitment.Id && date >= cp.StartDate && cp.Status == CommitmentPeriodStatus.Current) // TODO: ???
                        .SingleOrDefaultAsync(cancellationToken);

                    if (commitmentPeriod == null && date >= commitment.StartDate)
                    {
                        var lastCommitmentPeriod = await _dataContext.CommitmentPeriods
                            .Where(cp => cp.CommitmentId == commitment.Id)
                            .OrderByDescending(cp => cp.StartDate)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (lastCommitmentPeriod != null)
                        {
                            commitmentPeriod = new CommitmentPeriod
                            {
                                CommitmentId = commitment.Id,
                                StartDate = lastCommitmentPeriod.EndDate,
                                EndDate = lastCommitmentPeriod.EndDate.AddDays(commitment.Days),
                                Status = CommitmentPeriodStatus.Current
                            };
                        }
                        else
                        {
                            commitmentPeriod = new CommitmentPeriod
                            {
                                CommitmentId = commitment.Id,
                                StartDate = commitment.StartDate,
                                EndDate = commitment.StartDate.AddDays(commitment.Days),
                                Status = CommitmentPeriodStatus.Current
                            };
                        }

                        _dataContext.CommitmentPeriods.Add(commitmentPeriod);

                        foreach (var commitmentUser in commitment.Users.Where(u => metricName.All(mn => u.User.MetricProviders.Any(mp => mp.MetricName == mn))))
                        {
                            commitmentUser.User.Balance -= commitmentUser.Stake; // TODO: negative balance?

                            _dataContext.CommitmentPeriodUsers.Add(new CommitmentPeriodUser
                            {
                                CommitmentId = commitmentPeriod.CommitmentId,
                                StartDate = commitmentPeriod.StartDate,
                                EndDate = commitmentPeriod.EndDate,
                                UserId = commitmentUser.UserId,
                                Stake = commitmentUser.Stake,
                                Goals = commitment.Goals.Select(g => new CommitmentPeriodUserGoal
                                {
                                    CommitmentId = commitmentPeriod.CommitmentId,
                                    StartDate = commitmentPeriod.StartDate,
                                    EndDate = commitmentPeriod.EndDate,
                                    UserId = commitmentUser.UserId,
                                    MetricName = g.MetricName,
                                    MetricType = g.MetricType,
                                    ProviderName = commitmentUser.User.MetricProviders.Single(mp => mp.MetricName == g.MetricName).ProviderName,
                                }).ToList()
                            });
                        }

                        await _dataContext.SaveChangesAsync(cancellationToken);
                    }
                    else if (commitmentPeriod != null)
                    {
                        // HACK: for testing so users are added after a period starts
                        foreach (var commitmentUser in commitment.Users
                                     .Where(cu => commitmentPeriod.Users.All(cpu => cpu.UserId != cu.UserId))
                                     .Where(u => metricName.All(mn => u.User.MetricProviders.Any(mp => mp.MetricName == mn))))
                        {
                            commitmentUser.User.Balance -= commitmentUser.Stake;

                            _dataContext.CommitmentPeriodUsers.Add(new CommitmentPeriodUser
                            {
                                CommitmentId = commitmentPeriod.CommitmentId,
                                StartDate = commitmentPeriod.StartDate,
                                EndDate = commitmentPeriod.EndDate,
                                UserId = commitmentUser.UserId,
                                Stake = commitmentUser.Stake,
                                Goals = commitment.Goals.Select(g => new CommitmentPeriodUserGoal
                                {
                                    CommitmentId = commitmentPeriod.CommitmentId,
                                    StartDate = commitmentPeriod.StartDate,
                                    EndDate = commitmentPeriod.EndDate,
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

        public async Task UpdateCommitmentPeriodUserGoals(CancellationToken cancellationToken)
        {
            try
            {
                var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

                var commitmentPeriods = await _dataContext.CommitmentPeriods
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Users)
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Goals)
                    .Include(cp => cp.Users).ThenInclude(cpu => cpu.Goals)
                    .Where(cp => cp.StartDate <= date && cp.Status == CommitmentPeriodStatus.Current)
                    .ToListAsync(cancellationToken);

                foreach (var commitmentPeriod in commitmentPeriods)
                {
                    var startTime = commitmentPeriod.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                    var endTime = commitmentPeriod.EndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

                    foreach (var commitmentPeriodUser in commitmentPeriod.Users)
                    {
                        foreach (var commitmentPeriodUserGoals in commitmentPeriodUser.Goals.GroupBy(g => (g.MetricName, g.ProviderName)))
                        {
                            var userMetricProviderValues = await _dataContext.UserMetricProviderValues
                                .Where(umpv => umpv.UserId == commitmentPeriodUser.UserId && umpv.MetricName == commitmentPeriodUserGoals.Key.MetricName &&
                                               umpv.ProviderName == commitmentPeriodUserGoals.Key.ProviderName && umpv.Time >= startTime && umpv.Time < endTime)
                                .ToListAsync(cancellationToken);

                            foreach (var commitmentPeriodUserGoal in commitmentPeriodUserGoals)
                            {
                                var value = default(double?);
                                if (commitmentPeriodUserGoal.MetricType == MetricType.Count)
                                {
                                    var values = userMetricProviderValues.Where(upmv => upmv.MetricType == MetricType.Minutes).ToList();

                                    value = values.Any() ? values.Count : null;
                                }
                                else if (commitmentPeriodUserGoal.MetricType == MetricType.Minutes)
                                {
                                    var values = userMetricProviderValues.Where(upmv => upmv.MetricType == MetricType.Minutes).ToList();

                                    value = values.Any() ? Math.Round(values.Sum(upmv => upmv.Value), 2) : null;
                                }
                                else if (commitmentPeriodUserGoal.MetricType == MetricType.Value)
                                {
                                    var endUserProviderMetricValue = userMetricProviderValues.MaxBy(upmv => upmv.Time);

                                    if (endUserProviderMetricValue != null)
                                    {
                                        var startUserMetricProviderValue = await _dataContext.UserMetricProviderValues
                                            .Where(umpv => umpv.UserId == commitmentPeriodUser.UserId && umpv.MetricName == commitmentPeriodUserGoal.MetricName &&
                                                           umpv.ProviderName == commitmentPeriodUserGoal.ProviderName && umpv.MetricType == MetricType.Value && umpv.Time <= startTime)
                                            .OrderByDescending(upmv => upmv.Time)
                                            .FirstOrDefaultAsync(cancellationToken);

                                        if (startUserMetricProviderValue != null)
                                        {
                                            value = Math.Round(endUserProviderMetricValue.Value - startUserMetricProviderValue.Value, 1);
                                        }
                                    }
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

        // TODO: only complete once we know we have all the data
        public async Task UpdateCommitmentPeriods(CancellationToken cancellationToken)
        {
            try
            {
                var date = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

                var commitmentPeriods = await _dataContext.CommitmentPeriods
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Users)
                    .Include(cp => cp.Commitment).ThenInclude(c => c.Goals)
                    .Include(cp => cp.Users).ThenInclude(cpu => cpu.Goals).ThenInclude(cpug => cpug.Goal)
                    .Where(cp => cp.EndDate <= date && cp.Status == CommitmentPeriodStatus.Current)
                    .ToListAsync(cancellationToken);

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

                    if (commitmentPeriodUsersByResult[false].Any() || commitmentPeriodUsersByResult[true].Any())
                    {
                        var failedStake = commitmentPeriodUsersByResult[false].Sum(cpu => cpu.Stake);

                        if (commitmentPeriodUsersByResult[true].Any())
                        {
                            var rewardPerUser = (failedStake + commitmentPeriod.Commitment.Balance) / commitmentPeriodUsersByResult[true].Count;

                            foreach (var commitmentPeriodUser in commitmentPeriodUsersByResult[true])
                            {
                                commitmentPeriodUser.Reward = rewardPerUser + commitmentPeriodUser.Stake;

                                _dataContext.Entry(commitmentPeriodUser).State = EntityState.Modified;

                                var user = await _dataContext.Users.SingleAsync(u => u.Id == commitmentPeriodUser.UserId, cancellationToken);

                                user.Balance += rewardPerUser + commitmentPeriodUser.Stake;

                                _dataContext.Entry(user).State = EntityState.Modified;
                            }
                        }
                        else if (failedStake > 0)
                        {
                            commitmentPeriod.Commitment.Balance += commitmentPeriodUsersByResult[false].Sum(cpu => cpu.Stake);

                            _dataContext.Entry(commitmentPeriod.Commitment).State = EntityState.Modified;
                        }
                    }

                    commitmentPeriod.Status = CommitmentPeriodStatus.Complete;

                    _dataContext.Entry(commitmentPeriod).State = EntityState.Modified;
                }

                await _dataContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }
    }
}
