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

            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
