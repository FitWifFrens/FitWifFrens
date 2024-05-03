using FitWifFrens.Data;

namespace FitWifFrens.Playground
{
    public class Service : IHostedService
    {
        private readonly DataContext _dataContext;

        public Service(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _dataContext.Commitments.Add(new Commitment
            {
                Id = Guid.NewGuid(),
                ContractAddress = "6bb2344f-b6fb-4ac6-b08e-725f6ee35e9a"
            });

            await _dataContext.SaveChangesAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
