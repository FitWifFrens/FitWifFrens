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
                Image = "images/runner0.png",
                Amount = 2,
                Complete = true,
                ContractAddress = "0xDF5B443589e6a6f395602Baa722e906EF0e9f0e2",
                Providers = new List<CommitmentProvider>
                {
                    new CommitmentProvider
                    {
                        ProviderName = "Strava"
                    }
                }
            });

            // Fresh Contracts
            // 0xE0c9a330050d361b6024f99092E65D52ccfcfCd8
            // 0x7fAc91826D38CF8a70267848eE6f1017AB10423b
            // 0x6decaF380123802B47C6DEE671461D7dd65aE235
            // 0xd52e0a12ADF0e6Ba4b14D10449755245D1b3BA40
            // 0xDF5B443589e6a6f395602Baa722e906EF0e9f0e2

            var newCommitmentId = Guid.NewGuid();

            _dataContext.Commitments.Add(new Commitment
            {
                Id = newCommitmentId,
                Title = "90 minutes",
                Description = "Record 3 activities on Strava with a total time of 90 minutes",
                Image = "images/runner1.png",
                Amount = 5,
                ContractAddress = "0x2b937ba128d275E16E7f26De7d8524C21d0BB7cA", // 1
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
                Image = "images/runner2.png",
                Amount = 10,
                ContractAddress = "0x6cC16743203C694f44Cf2698aa2537561832061e", // 2
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
                Image = "images/developer0.png",
                Amount = 42,
                ContractAddress = "0x947384ef21BB443416383A7FFeF3f1C3543c19eD",
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
