using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Background
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddHostedService<StravaService>();

            var app = builder.Build();

            app.Run();
        }
    }
}
