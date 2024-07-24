using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration.AddUserSecrets<Program>();

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

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            });

            builder.Services.AddIdentityCore<User>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<DataContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            //builder.Services.AddHostedService<RecreateService>();
            //builder.Services.AddHostedService<UpdateService>();

            var app = builder.Build();

            app.Run();
        }
    }
}
