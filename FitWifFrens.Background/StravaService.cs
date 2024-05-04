using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;
using StravaSharp;

namespace FitWifFrens.Background
{
    public class StravaService : IHostedService
    {
        private readonly DataContext _dataContext;

        public StravaService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var user in _dataContext.Users.Include(u => u.Tokens))
            {
                var stravaToken = user.Tokens.SingleOrDefault(u => u.LoginProvider == "Strava" && u.Name == "access_token");

                if (stravaToken != null)
                {
                    var client = new Client(new StravaAuthenticator(stravaToken.Value!));

                    var athlete = await client.Athletes.GetCurrent();

                    var activities = await client.Activities.GetAthleteActivities();

                    var activityMine = await client.Activities.Get(11302245706);

                    var activityOther = await client.Activities.Get(11325887240);

                    ;
                }
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
