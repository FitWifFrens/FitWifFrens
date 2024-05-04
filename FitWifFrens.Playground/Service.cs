using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    public class Service : IHostedService
    {
        private readonly record struct NewUser(Guid Id, string Email, string WorldId, bool CompletedOldCommitment, bool JoinNewCommitment);

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
                new NewUser(Guid.NewGuid(), "test1@gmail.com", "0x2fefb77e01d5019b7b2571b44d8cea84d0e1d83491d93ec3f9bb8871dedd7cdb", true, false),
                new NewUser(Guid.NewGuid(), "test2@gmail.com", "0x1a66e7598f54a728f7db97983b226005aca9ecbc44928e519f0244a61303af20", true, true),
                new NewUser(Guid.NewGuid(), "test3@gmail.com", "0x035646cc875f3b69fb4fbbb0e8a6f01805a52255d7619a0fddb71cea7ad49960", false, true),
            };

            var oldCommitmentId = Guid.NewGuid();

            _dataContext.Commitments.Add(new Commitment
            {
                Id = oldCommitmentId,
                Title = "60 minutes",
                Description = "Record 2 activities on Strava with a total time of 60 minutes",
                Image = "https://cdn.shopify.com/s/files/1/0942/6160/files/marathon-des-sables-desert-running.jpg?v=1531298303",
                Amount = 2,
                Complete = true,
                ContractAddress = "0x93F682be07C5772390FfB44585365Cf64e347331",
                Providers = new List<CommitmentProvider>
                {
                    new CommitmentProvider
                    {
                        ProviderName = "Strava"
                    }
                }
            });


            var newCommitmentId = Guid.NewGuid();

            _dataContext.Commitments.Add(new Commitment
            {
                Id = newCommitmentId,
                Title = "90 minutes",
                Description = "Record 3 activities on Strava with a total time of 90 minutes",
                Image = "https://cdn.shopify.com/s/files/1/0942/6160/files/running-facts-crazy.jpg?v=1531298463",
                Amount = 5,
                ContractAddress = "0x93F682be07C5772390FfB44585365Cf64e347331",
                Providers = new List<CommitmentProvider>
                {
                    new CommitmentProvider
                    {
                        ProviderName = "Strava"
                    }
                }
            });

            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.NewGuid(),
                Title = "120 minutes",
                Description = "Record 4 activities on Strava with a total time of 120 minutes",
                Image = "https://cdn.shopify.com/s/files/1/0942/6160/files/running-backwards.jpg?v=1531298209",
                Amount = 10,
                ContractAddress = "0x93F682be07C5772390FfB44585365Cf64e347331",
                Providers = new List<CommitmentProvider>
                {
                    new CommitmentProvider
                    {
                        ProviderName = "Strava"
                    }
                }
            });

            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.NewGuid(),
                Title = "Good Developer",
                Description = "Answer 2 StackExchange questions a week",
                Image = "https://pedrorijo.com/assets/img/super_developer.png",
                Amount = 42,
                ContractAddress = "0x93F682be07C5772390FfB44585365Cf64e347331",
                Providers = new List<CommitmentProvider>
                {
                    new CommitmentProvider
                    {
                        ProviderName = "StackExchange"
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
                }, CancellationToken.None);

                _dataContext.UserLogins.Add(new UserLogin
                {
                    LoginProvider = "WorldId",
                    ProviderKey = newUser.WorldId,
                    ProviderDisplayName = "World ID",
                    UserId = newUser.Id.ToString()
                });

                if (newUser.CompletedOldCommitment)
                {
                    _dataContext.CommittedUsers.Add(new CommittedUser
                    {
                        CommitmentId = oldCommitmentId,
                        UserId = newUser.Id.ToString(),
                        Transaction = Guid.NewGuid().ToString(),
                        DistributedAmount = 3
                    });
                }
                else
                {
                    _dataContext.CommittedUsers.Add(new CommittedUser
                    {
                        CommitmentId = oldCommitmentId,
                        UserId = newUser.Id.ToString(),
                        Transaction = Guid.NewGuid().ToString(),
                        DistributedAmount = 0
                    });
                }

                if (newUser.JoinNewCommitment)
                {
                    _dataContext.CommittedUsers.Add(new CommittedUser
                    {
                        CommitmentId = newCommitmentId,
                        UserId = newUser.Id.ToString(),
                        Transaction = Guid.NewGuid().ToString()
                    });
                }

                await _dataContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
