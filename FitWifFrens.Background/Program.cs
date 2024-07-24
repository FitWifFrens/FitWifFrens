using FitWifFrens.Data;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Background
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration.AddUserSecrets<Program>();

            builder.Services.AddSingleton(TimeProvider.System);

            var postgresConnection = builder.Configuration.GetConnectionString("PostgresConnection") ?? throw new InvalidOperationException("Connection string 'PostgresConnection' not found.");
            builder.Services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(postgresConnection, o =>
                {
#if DEBUG
                    o.SetPostgresVersion(11, 0);
#else
                    o.SetPostgresVersion(16, 3);
#endif
                }));

            builder.Services.AddHttpClient();

            builder.Services.AddHangfire(configuration =>
            {
                //configuration.UseRedisStorage(redisConnectionString);
                configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
                configuration.UseSimpleAssemblyNameTypeSerializer();
                configuration.UseRecommendedSerializerSettings();
                configuration.UsePostgreSqlStorage(postgresConnection);
            });
            builder.Services.AddHangfireServer();

            builder.Services.AddHostedService<StravaService>();
            builder.Services.AddHostedService<WithingsService>();

            builder.Services.AddHostedService<CommitmentPeriodService>();

            var app = builder.Build();

            app.Run();
        }
    }
}
