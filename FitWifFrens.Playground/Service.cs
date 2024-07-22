using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    public class Service : IHostedService
    {
        private readonly record struct NewUser(Guid Id, string Email, bool CompletedOldCommitment, bool JoinNewCommitment);

        private readonly IUserStore<User> _userStore;
        private readonly DataContext _dataContext;

        public Service(IUserStore<User> userStore, DataContext dataContext)
        {
            _userStore = userStore;
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
                Description = "Record 2 activities on Strava with a total time of 60 minutes",
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
                Periods = new List<CommitmentPeriod>
                {
                    new CommitmentPeriod
                    {
                        StartDate = new DateOnly(2024, 07, 15),
                        EndDate = new DateOnly(2024, 07, 22),
                    },
                    new CommitmentPeriod
                    {
                        StartDate = new DateOnly(2024, 07, 22),
                        EndDate = new DateOnly(2024, 07, 29),
                    }
                }
            });

            var commitment2Id = Guid.Parse("46c06bf6-2e22-4e00-bf29-05050485d9a0");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment2Id,
                Title = "90 minutes",
                Description = "Record 3 activities on Strava with a total time of 90 minutes",
                Image = "images/runner1.png",
                StartDate = new DateOnly(2024, 07, 29),
                Days = 7,
                ContractAddress = "0x2b937ba128d275E16E7f26De7d8524C21d0BB7cA", // 1
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
                Periods = new List<CommitmentPeriod>
                {
                    new CommitmentPeriod
                    {
                        StartDate = new DateOnly(2024, 07, 29),
                        EndDate = new DateOnly(2024, 08, 05),
                    }
                }
            });

            var commitment3Id = Guid.Parse("4d9a6fa4-6903-49dc-86a1-4780b826cd2a");

            _dataContext.Commitments.Add(new Commitment
            {
                Id = commitment3Id,
                Title = "Weight Loss",
                Description = "Lose at least a kilogram every 2 weeks",
                Image = "images/developer0.png",
                StartDate = new DateOnly(2024, 07, 22),
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
                        Value = 1
                    }
                },
                Periods = new List<CommitmentPeriod>
                {
                    new CommitmentPeriod
                    {
                        StartDate = new DateOnly(2024, 07, 22),
                        EndDate = new DateOnly(2024, 08, 05),
                    },
                    new CommitmentPeriod
                    {
                        StartDate = new DateOnly(2024, 08, 05),
                        EndDate = new DateOnly(2024, 08, 19),
                    }
                }
            });

            await _dataContext.SaveChangesAsync(CancellationToken.None);

            foreach (var newUser in users)
            {
                await _userStore.CreateAsync(new User
                {
                    Id = newUser.Id.ToString(),
                    Email = newUser.Email,
                    Balance = 90
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

                _dataContext.CommitmentPeriodUsers.Add(new CommitmentPeriodUser
                {
                    CommitmentId = commitment1Id,
                    UserId = newUser.Id.ToString(),
                    StartDate = new DateOnly(2024, 07, 15),
                    EndDate = new DateOnly(2024, 07, 22),
                    Stake = 10,
                });

                //if (newUser.CompletedOldCommitment)
                //{
                //    _dataContext.CommittedUsers.Add(new CommitmentUser
                //    {
                //        CommitmentId = oldCommitmentId,
                //        UserId = newUser.Id.ToString(),
                //        Transaction = Guid.NewGuid().ToString(),
                //        DistributedAmount = 3
                //    });
                //}
                //else
                //{
                //    _dataContext.CommittedUsers.Add(new CommitmentUser
                //    {
                //        CommitmentId = oldCommitmentId,
                //        UserId = newUser.Id.ToString(),
                //        Transaction = Guid.NewGuid().ToString(),
                //        DistributedAmount = 0
                //    });
                //}

                //if (newUser.JoinNewCommitment)
                //{
                //    _dataContext.CommittedUsers.Add(new CommitmentUser
                //    {
                //        CommitmentId = newCommitmentId,
                //        UserId = newUser.Id.ToString(),
                //        Transaction = Guid.NewGuid().ToString()
                //    });
                //}

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
