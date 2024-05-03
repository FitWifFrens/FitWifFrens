using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddHostedService<Service>();

            var app = builder.Build();

            app.Run();
        }
    }
}
