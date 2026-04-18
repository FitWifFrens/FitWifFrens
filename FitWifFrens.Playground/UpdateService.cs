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

            // var users = await _dataContext.Users.Where(u => u.Balance <= 0).ToListAsync(cancellationToken);
            //
            // foreach (var user in users)
            // {
            //     user.Balance += 100;
            //
            //     _dataContext.Entry(user).State = EntityState.Modified;
            //
            //     _dataContext.Deposits.Add(new Deposit
            //     {
            //         Transaction = "0x" + Guid.NewGuid().ToString().Replace("-", string.Empty),
            //         UserId = user.Id,
            //         Amount = 100,
            //         Time = DateTime.UtcNow
            //     });
            // }
            
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
                new Metric
                {
                    Name = "Cycling"
                },
            });

            _dataContext.MetricProviders.AddRange(new List<MetricProvider>
            {
                new MetricProvider
                {
                    ProviderName = "Telegram",
                    MetricName = "Telegram Poll",
                },
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
                    MetricName = "Telegram Poll",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Telegram Poll",
                    Type = MetricType.Value
                },
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

            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.Parse("b8f3a1d2-2e44-4b7c-9d5a-3c1e7f8a9b42"),
                Title = "120 minutes",
                Description = "Record 3 rides with a total time of 120 minutes",
                Image = "images/cycling0.png",
                StartDate = new DateOnly(2026, 03, 23),
                Days = 7,
                ContractAddress = "0x1a2b3c4d5e6f7890aBcDeF1234567890AbCdEf12",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Cycling",
                        MetricType = MetricType.Minutes,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 120
                    },
                    new Goal
                    {
                        MetricName = "Cycling",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 3
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
