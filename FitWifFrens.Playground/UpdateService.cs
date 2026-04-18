using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    public class UpdateService : IHostedService
    {
        private readonly DataContext _dataContext;

        public UpdateService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _dataContext.Database.MigrateAsync(cancellationToken);

            // await UpdateBalances(cancellationToken);
            // await UpdateTelegram(cancellationToken);
            await UpdateCycling(cancellationToken);

            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task UpdateBalances(CancellationToken cancellationToken)
        {
            var users = await _dataContext.Users.Where(u => u.Balance <= 0).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.Balance += 100;

                _dataContext.Entry(user).State = EntityState.Modified;

                _dataContext.Deposits.Add(new Deposit
                {
                    Transaction = "0x" + Guid.NewGuid().ToString().Replace("-", string.Empty),
                    UserId = user.Id,
                    Amount = 100,
                    Time = DateTime.UtcNow
                });
            }
        }

        private Task UpdateTelegram(CancellationToken cancellationToken)
        {
            _dataContext.Providers.AddRange(new List<Provider>
            {
                new Provider
                {
                    Name = "Telegram"
                },
            });

            _dataContext.Metrics.AddRange(new List<Metric>
            {
                new Metric
                {
                    Name = "Telegram Poll"
                },
            });

            _dataContext.MetricProviders.AddRange(new List<MetricProvider>
            {
                new MetricProvider
                {
                    ProviderName = "Telegram",
                    MetricName = "Telegram Poll",
                },
            });

            _dataContext.MetricValues.AddRange(new List<MetricValue>
            {
                new MetricValue
                {
                    MetricName = "Telegram Poll",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Telegram Poll",
                    Type = MetricType.Value
                },
            });

            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.Parse("07c4559b-e5b5-4f77-a3c3-d9c0654298b8"),
                Title = "Daily Diet Check-In",
                Description = "Rate your diet each day. Win by logging every day and averaging better than holding steady.",
                Image = "images/weight-loss1.png",
                StartDate = new DateOnly(2026, 03, 23),
                Days = 7,
                ContractAddress = "0x142384ef21BB443416383A7FFeF3f1C3543c19eD",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Telegram Poll",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 7
                    },
                    new Goal
                    {
                        MetricName = "Telegram Poll",
                        MetricType = MetricType.Value,
                        Rule = GoalRule.GreaterThan,
                        Value = 3
                    }
                },
                TelegramPollRule = new CommitmentTelegramPollRule
                {
                    Question = "How do you rate your diet?",
                    RequireDailyResponses = true,
                    AllowsMultipleAnswers = false,
                    IsAnonymous = false,
                    Options = new List<CommitmentTelegramPollRuleOption>
                    {
                        new CommitmentTelegramPollRuleOption
                        {
                            Index = 0,
                            Text = "So clean my scale sends thank-you notes (losing weight)",
                            Value = 5
                        },
                        new CommitmentTelegramPollRuleOption
                        {
                            Index = 1,
                            Text = "Mostly solid, with tiny snack crimes (losing weight slowly)",
                            Value = 4
                        },
                        new CommitmentTelegramPollRuleOption
                        {
                            Index = 2,
                            Text = "Salad by day, snack goblin by night (holding steady)",
                            Value = 3
                        },
                        new CommitmentTelegramPollRuleOption
                        {
                            Index = 3,
                            Text = "Accidental bulk mode activated (getting fatter)",
                            Value = 2
                        },
                        new CommitmentTelegramPollRuleOption
                        {
                            Index = 4,
                            Text = "My meal plan is chaos and cheese (getting fatter fast)",
                            Value = 1
                        }
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            return Task.CompletedTask;
        }

        private async Task UpdateCycling(CancellationToken cancellationToken)
        {
            _dataContext.Metrics.AddRange(new List<Metric>
            {
                new Metric
                {
                    Name = "Cycling"
                },
            });

            _dataContext.MetricProviders.AddRange(new List<MetricProvider>
            {
                new MetricProvider
                {
                    ProviderName = "Strava",
                    MetricName = "Cycling",
                },
                new MetricProvider
                {
                    ProviderName = "Withings",
                    MetricName = "Cycling",
                },
            });

            _dataContext.MetricValues.AddRange(new List<MetricValue>
            {
                new MetricValue
                {
                    MetricName = "Cycling",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Cycling",
                    Type = MetricType.Minutes
                },
            });

            foreach (var providerName in new[] { "Strava", "Withings" })
            {
                var userIds = await _dataContext.Users
                    .Where(u => u.Logins.Any(l => l.LoginProvider == providerName))
                    .Where(u => !u.MetricProviders.Any(ump => ump.MetricName == "Cycling" && ump.ProviderName == providerName))
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                foreach (var userId in userIds)
                {
                    _dataContext.UserMetricProviders.Add(new UserMetricProvider
                    {
                        UserId = userId,
                        MetricName = "Cycling",
                        ProviderName = providerName,
                    });
                }
            }

            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.Parse("9f528253-67ee-443e-99fb-0b776faaa66f"),
                Title = "90 minutes",
                Description = "Record 2 rides with a total time of 90 minutes",
                Image = "images/cycling0.png",
                StartDate = new DateOnly(2026, 04, 13),
                Days = 7,
                ContractAddress = "0x142384ef21BB443416383A7FFeF3f1C3543c19eD",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Cycling",
                        MetricType = MetricType.Minutes,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 90
                    },
                    new Goal
                    {
                        MetricName = "Cycling",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 2
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });
        }
    }
}
