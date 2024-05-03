using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Playground
{
    public class Service : IHostedService
    {
        private readonly IUserStore<User> _userStore;
        private readonly DataContext _dataContext;

        public Service(IUserStore<User> userStore, DataContext dataContext)
        {
            _userStore = userStore;
            _dataContext = dataContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var userId1 = Guid.NewGuid().ToString();

            _ = await _userStore.CreateAsync(new User
            {
                Id = userId1,
                Email = "test1@gmail.com",
            }, CancellationToken.None);

            var userId2 = Guid.NewGuid().ToString();

            _ = await _userStore.CreateAsync(new User
            {
                Id = userId2,
                Email = "test2@gmail.com",
            }, CancellationToken.None);

            var userId3 = Guid.NewGuid().ToString();

            _ = await _userStore.CreateAsync(new User
            {
                Id = userId3,
                Email = "test3@gmail.com",
            }, CancellationToken.None);

            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.NewGuid(),
                Title = "90 minutes",
                Description = "Record 3 activities on Strava with a total time of 90 minutes",
                Image = "https://cdn.shopify.com/s/files/1/0942/6160/files/running-facts-crazy.jpg?v=1531298463",
                Amount = 5,
                ContractAddress = "6bb2344f-b6fb-4ac6-b08e-725f6ee35e9a",
            });

            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.NewGuid(),
                Title = "120 minutes",
                Description = "Record 4 activities on Strava with a total time of 120 minutes",
                Image = "https://cdn.shopify.com/s/files/1/0942/6160/files/running-backwards.jpg?v=1531298209",
                Amount = 10,
                ContractAddress = "f5baa36e-8ca9-40c8-86ee-d35824c772c3",
            });

            await _dataContext.SaveChangesAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
