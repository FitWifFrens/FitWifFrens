using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;

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
            await _dataContext.Database.EnsureCreatedAsync(cancellationToken);


            var email = "chad@fitwiffrens.com";
            var password = "Pass!234";
            var user = new User
            {
                Id = "65c79331-17f4-498c-bd91-7236518324ee",
            };

            await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await _userManager.SetEmailAsync(user, email);
            var result = await _userManager.CreateAsync(user, password);

            if (result != IdentityResult.Success)
            {
                throw new Exception("104644b7-c482-49a0-b6d7-502ed79ea594");
            }

            await _dataContext.SaveChangesAsync(CancellationToken.None);

            Console.WriteLine("Done...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
