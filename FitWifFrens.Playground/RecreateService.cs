using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    public class RecreateService : IHostedService
    {
        private readonly IUserStore<User> _userStore;
        private readonly UserManager<User> _userManager;
        private readonly DataContext _dataContext;

        public RecreateService(IUserStore<User> userStore, UserManager<User> UserManager, DataContext dataContext)
        {
            _userStore = userStore;
            _userManager = UserManager;
            _dataContext = dataContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _dataContext.Database.EnsureDeletedAsync(cancellationToken);
            await _dataContext.Database.MigrateAsync(cancellationToken);


            _dataContext.Providers.AddRange(new List<Provider>
            {
                new Provider
                {
                    Name = "Microsoft"
                },
                new Provider
                {
                    Name = "Strava"
                },
                new Provider
                {
                    Name = "Withings"
                },
                new Provider
                {
                    Name = "Google"
                },
                new Provider
                {
                    Name = "Facebook"
                },
            });

            _dataContext.Metrics.AddRange(new List<Metric>
            {
                new Metric
                {
                    Name = "Blood Pressure"
                },
                new Metric
                {
                    Name = "Exercise"
                },
                new Metric
                {
                    Name = "Running"
                },
                new Metric
                {
                    Name = "Workout"
                },
                new Metric
                {
                    Name = "Weight"
                },
                new Metric
                {
                    Name = "Tasks"
                },
            });

            _dataContext.MetricProviders.AddRange(new List<MetricProvider>
            {
                new MetricProvider
                {
                    ProviderName = "Microsoft",
                    MetricName = "Tasks",
                },
                new MetricProvider
                {
                    ProviderName = "Strava",
                    MetricName = "Exercise",
                },
                new MetricProvider
                {
                    ProviderName = "Strava",
                    MetricName = "Running",
                },
                new MetricProvider
                {
                    ProviderName = "Strava",
                    MetricName = "Workout",
                },

                new MetricProvider
                {
                    ProviderName = "Withings",
                    MetricName = "Blood Pressure",
                },
                new MetricProvider
                {
                    ProviderName = "Withings",
                    MetricName = "Exercise",
                },
                new MetricProvider
                {
                    ProviderName = "Withings",
                    MetricName = "Running",
                },
                new MetricProvider
                {
                    ProviderName = "Withings",
                    MetricName = "Workout",
                },
                new MetricProvider
                {
                    ProviderName = "Withings",
                    MetricName = "Weight",
                },
            });


            _dataContext.MetricValues.AddRange(new List<MetricValue>
            {
                new MetricValue
                {
                    MetricName = "Blood Pressure",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Tasks",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Exercise",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Exercise",
                    Type = MetricType.Minutes
                },
                new MetricValue
                {
                    MetricName = "Running",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Running",
                    Type = MetricType.Minutes
                },
                new MetricValue
                {
                    MetricName = "Workout",
                    Type = MetricType.Count
                },
                new MetricValue
                {
                    MetricName = "Workout",
                    Type = MetricType.Minutes
                },
                new MetricValue
                {
                    MetricName = "Weight",
                    Type = MetricType.Value
                },
            });

            await _dataContext.SaveChangesAsync(CancellationToken.None);

            var commitment1Id = Guid.Parse("f0ba65f0-b1bb-4a12-82e8-0a2c61e027a4");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment1Id,
                Title = "120 minutes",
                Description = "Record 3 runs with a total time of 120 minutes",
                Image = "images/runner0.png",
                StartDate = new DateOnly(2024, 07, 22),
                Days = 7,
                ContractAddress = "0xDF5B443589e6a6f395602Baa722e906EF0e9f0e2",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Running",
                        MetricType = MetricType.Minutes,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 120
                    },
                    new Goal
                    {
                        MetricName = "Running",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 3
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            var commitment2Id = Guid.Parse("46c06bf6-2e22-4e00-bf29-05050485d9a0");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment2Id,
                Title = "180 minutes",
                Description = "Record 4 runs with a total time of 180 minutes",
                Image = "images/runner2.png",
                StartDate = new DateOnly(2024, 07, 22),
                Days = 7,
                ContractAddress = "0x2b937ba128d275E16E7f26De7d8524C21d0BB7cA",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Running",
                        MetricType = MetricType.Minutes,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 180
                    },
                    new Goal
                    {
                        MetricName = "Running",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 4
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });


            var commitment3Id = Guid.Parse("20398161-7de8-4cdb-929a-165fe1e892da");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment3Id,
                Title = "3 workouts",
                Description = "Record 3 workouts every week",
                Image = "images/runner1.png",
                StartDate = new DateOnly(2024, 07, 22),
                Days = 7,
                ContractAddress = "0x5b934ba128d275E16E7f26De7d8524C21d0BB7cA",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Workout",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 3
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            var commitment4Id = Guid.Parse("4d9a6fa4-6903-49dc-86a1-4780b826cd2a");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment4Id,
                Title = "Weight Loss",
                Description = "Lose at least half a kilogram every 2 weeks",
                Image = "images/weight-loss0.png",
                StartDate = new DateOnly(2024, 07, 22),
                Days = 14,
                ContractAddress = "0x947384ef21BB443416383A7FFeF3f1C3543c19eD",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Weight",
                        MetricType = MetricType.Value,
                        Rule = GoalRule.LessThanOrEqualTo,
                        Value = -0.5
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            var commitment5Id = Guid.Parse("86913f20-0fae-49cc-acb0-edfb34360fee");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment5Id,
                Title = "Blood Pressure",
                Description = "Measure your blood pressure at least once every 3 days",
                Image = "images/blood-pressure0.png",
                StartDate = new DateOnly(2024, 08, 05),
                Days = 3,
                ContractAddress = "0x344384ef21BB443416383A7FFeF3f1C3543c19eD",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        MetricName = "Blood Pressure",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 1
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            await _dataContext.SaveChangesAsync(CancellationToken.None);


            var email = "chad@fitwiffrens.com";
            var password = "Pass!234";
            var user = new User
            {
                Id = "65c79331-17f4-498c-bd91-7236518324ee",
                Balance = 100
            };

            await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await _userManager.SetEmailAsync(user, email);
            var result = await _userManager.CreateAsync(user, password);

            if (result != IdentityResult.Success)
            {
                throw new Exception("104644b7-c482-49a0-b6d7-502ed79ea594");
            }

            _dataContext.Deposits.Add(new Deposit
            {
                Transaction = "0x" + Guid.NewGuid().ToString().Replace("-", string.Empty),
                UserId = user.Id,
                Amount = 100,
                Time = DateTime.UtcNow
            });

            _dataContext.Displays.Add(new Display
            {
                MacAddress = "B0:B2:1C:50:6E:FC",
                User = new UserDisplay
                {
                    UserId = user.Id
                }
            });

            await _dataContext.SaveChangesAsync(CancellationToken.None);

            Console.WriteLine("Done...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
