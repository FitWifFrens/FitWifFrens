using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Background
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration.AddUserSecrets<Program>();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(connectionString, o => o.SetPostgresVersion(11, 0)));

            builder.Services.AddHostedService<StravaService>();

            var app = builder.Build();

            app.Run();
        }
    }
}
