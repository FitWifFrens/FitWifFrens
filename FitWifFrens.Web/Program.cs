using AspNet.Security.OAuth.Strava;
using AspNet.Security.OAuth.Withings;
using FitWifFrens.Data;
using FitWifFrens.Web.Background;
using FitWifFrens.Web.Components;
using FitWifFrens.Web.Components.Account;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nethereum.Metamask;
using Nethereum.Metamask.Blazor;
using Nethereum.UI;

namespace FitWifFrens.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddLocalization();

            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.EnablePerformanceCounterCollectionModule = false;
                options.EnableRequestTrackingTelemetryModule = false;
                options.EnableEventCounterCollectionModule = false;
                options.EnableDependencyTrackingTelemetryModule = false;
                options.EnableAppServicesHeartbeatTelemetryModule = false;
                options.EnableAzureInstanceMetadataTelemetryModule = false;
                options.EnableQuickPulseMetricStream = false;
                options.EnableAdaptiveSampling = true;
                options.EnableHeartbeat = false;
                options.AddAutoCollectedMetricExtractor = false;
                options.EnableDiagnosticsTelemetryModule = false;
            });

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            builder.Services.AddControllers();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddStrava(options =>
                {
                    options.ClientId = builder.Configuration.GetValue<string>("Authentication:Strava:ClientId")!;
                    options.ClientSecret = builder.Configuration.GetValue<string>("Authentication:Strava:ClientSecret")!;

                    options.Scope.Add("read_all");
                    options.Scope.Add("profile:read_all");
                    options.Scope.Add("activity:read_all");

                    options.SaveTokens = true;
                })
                .AddWithings(options =>
                {
                    options.ClientId = builder.Configuration.GetValue<string>("Authentication:Withings:ClientId")!;
                    options.ClientSecret = builder.Configuration.GetValue<string>("Authentication:Withings:ClientSecret")!;

                    options.Scope.Add("user.metrics");
                    options.Scope.Add("user.activity");

                    options.SaveTokens = true;
                })
                .AddIdentityCookies(options =>
                {
                    options.ApplicationCookie!.Configure(cookieOptions =>
                    {
                        cookieOptions.ExpireTimeSpan = TimeSpan.FromDays(14);
                        cookieOptions.SlidingExpiration = true;
                    });
                });

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
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<User>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<DataContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<User>, IdentityNoOpEmailSender>();

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<AutomaticRetryAttribute>(new AutomaticRetryAttribute { Attempts = 3 });
            builder.Services.AddHangfire((provider, configuration) =>
            {
                configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
                configuration.UseSimpleAssemblyNameTypeSerializer();
                configuration.UseRecommendedSerializerSettings();
                configuration.UseFilter(provider.GetRequiredService<AutomaticRetryAttribute>());
                configuration.UsePostgreSqlStorage(postgresConnection);
            });
            builder.Services.AddHangfireServer();

            builder.Services.AddSingleton<RefreshTokenServiceConfiguration>(new RefreshTokenServiceConfiguration
            {
                Strava = new RefreshTokenServiceConfiguration.RefreshTokenConfiguration(
                    StravaAuthenticationDefaults.TokenEndpoint,
                    builder.Configuration.GetValue<string>("Authentication:Strava:ClientId")!,
                    builder.Configuration.GetValue<string>("Authentication:Strava:ClientSecret")!),
                Withings = new RefreshTokenServiceConfiguration.RefreshTokenConfiguration(
                    WithingsAuthenticationDefaults.TokenEndpoint,
                    builder.Configuration.GetValue<string>("Authentication:Withings:ClientId")!,
                    builder.Configuration.GetValue<string>("Authentication:Withings:ClientSecret")!)
            });
            builder.Services.AddScoped<RefreshTokenService>(); // TODO: singleton?

            builder.Services.AddHostedService<JobService>();

            builder.Services.AddScoped<StravaService>();
            builder.Services.AddScoped<WithingsService>();
            builder.Services.AddScoped<CommitmentPeriodService>();

            builder.Services.AddScoped<IMetamaskInterop, MetamaskBlazorInterop>();
            builder.Services.AddScoped<MetamaskHostProvider>();
            //Add metamask as the selected ethereum host provider
            builder.Services.AddScoped(services =>
            {
                var metamaskHostProvider = services.GetService<MetamaskHostProvider>();
                var selectedHostProvider = new SelectedEthereumHostProviderService();
                selectedHostProvider.SetSelectedEthereumHostProvider(metamaskHostProvider);
                return selectedHostProvider;
            });

            builder.Services.AddScoped<IEthereumHostProvider, MetamaskHostProvider>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new DashboardAuthorizationFilter()]
            });

            app.Run();
        }
    }
}
