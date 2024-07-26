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

            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
