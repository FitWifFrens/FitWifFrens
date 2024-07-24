using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    public class RecreateService : IHostedService
    {
        private readonly record struct NewUser(Guid Id, string Email, bool CompletedOldCommitment, bool JoinNewCommitment);

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

            var users = new List<NewUser>
            {
                new NewUser(Guid.NewGuid(), "test1@gmail.com", true, false),
                new NewUser(Guid.NewGuid(), "test2@gmail.com", true, true),
                new NewUser(Guid.NewGuid(), "test3@gmail.com", false, true),
            };

            var commitment1Id = Guid.Parse("f0ba65f0-b1bb-4a12-82e8-0a2c61e027a4");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment1Id,
                Title = "60 minutes",
                Description = "Record 2 runs on Strava with a total time of 60 minutes",
                Image = "images/runner0.png",
                StartDate = new DateOnly(2024, 07, 15),
                Days = 7,
                ContractAddress = "0xDF5B443589e6a6f395602Baa722e906EF0e9f0e2",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        ProviderName = "Strava",
                        MetricName = "Running",
                        MetricType = MetricType.Minutes,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 60
                    },
                    new Goal
                    {
                        ProviderName = "Strava",
                        MetricName = "Running",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 2
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            var commitment2Id = Guid.Parse("46c06bf6-2e22-4e00-bf29-05050485d9a0");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment2Id,
                Title = "90 minutes",
                Description = "Record 3 runs on Strava with a total time of 90 minutes",
                Image = "images/runner1.png",
                StartDate = new DateOnly(2024, 07, 15),
                Days = 7,
                ContractAddress = "0x2b937ba128d275E16E7f26De7d8524C21d0BB7cA",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        ProviderName = "Strava",
                        MetricName = "Running",
                        MetricType = MetricType.Minutes,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 90
                    },
                    new Goal
                    {
                        ProviderName = "Strava",
                        MetricName = "Running",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 3
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });


            var commitment3Id = Guid.Parse("20398161-7de8-4cdb-929a-165fe1e892da");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment3Id,
                Title = "4 workouts",
                Description = "Record 4 workouts on Strava every week",
                Image = "images/runner2.png",
                StartDate = new DateOnly(2024, 07, 22),
                Days = 7,
                ContractAddress = "0x5b934ba128d275E16E7f26De7d8524C21d0BB7cA",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        ProviderName = "Strava",
                        MetricName = "Workout",
                        MetricType = MetricType.Count,
                        Rule = GoalRule.GreaterThanOrEqualTo,
                        Value = 4
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            var commitment4Id = Guid.Parse("4d9a6fa4-6903-49dc-86a1-4780b826cd2a");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment4Id,
                Title = "Weight Loss",
                Description = "Lose at least a kilogram every 2 weeks according to Withings",
                Image = "images/developer0.png",
                StartDate = new DateOnly(2024, 07, 08),
                Days = 14,
                ContractAddress = "0x947384ef21BB443416383A7FFeF3f1C3543c19eD",
                Goals = new List<Goal>
                {
                    new Goal
                    {
                        ProviderName = "Withings",
                        MetricName = "Weight",
                        MetricType = MetricType.Value,
                        Rule = GoalRule.LessThanOrEqualTo,
                        Value = -1
                    }
                },
                Periods = new List<CommitmentPeriod>()
            });

            await _dataContext.SaveChangesAsync(CancellationToken.None);


            var email = "didge1987@gmail.com";
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
                Amount = 100
            });

            _dataContext.CommitmentUsers.Add(new CommitmentUser
            {
                CommitmentId = commitment1Id,
                UserId = user.Id,
                Stake = 10,
            });

            _dataContext.CommitmentUsers.Add(new CommitmentUser
            {
                CommitmentId = commitment4Id,
                UserId = user.Id,
                Stake = 20,
            });

            foreach (var newUser in users)
            {
                await _userStore.CreateAsync(new User
                {
                    Id = newUser.Id.ToString(),
                    Email = newUser.Email,
                    Balance = 100
                }, CancellationToken.None);

                _dataContext.Deposits.Add(new Deposit
                {
                    Transaction = "0x" + Guid.NewGuid().ToString().Replace("-", string.Empty),
                    UserId = newUser.Id.ToString(),
                    Amount = 100
                });

                _dataContext.CommitmentUsers.Add(new CommitmentUser
                {
                    CommitmentId = commitment1Id,
                    UserId = newUser.Id.ToString(),
                    Stake = 10,
                });

                _dataContext.CommitmentUsers.Add(new CommitmentUser
                {
                    CommitmentId = commitment2Id,
                    UserId = newUser.Id.ToString(),
                    Stake = 20,
                });

                await _dataContext.SaveChangesAsync(CancellationToken.None);
            }

            Console.WriteLine("Done...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
