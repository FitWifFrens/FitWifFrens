using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Data
{
    public class DataContext : IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<Metric> Metrics { get; set; }
        public DbSet<MetricValue> MetricValues { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<ProviderMetricValue> ProviderMetricValues { get; set; }
        public DbSet<Commitment> Commitments { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<CommitmentPeriod> CommitmentPeriods { get; set; }
        public DbSet<CommitmentUser> CommitmentUsers { get; set; }
        public DbSet<CommitmentPeriodUser> CommitmentPeriodUsers { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Properties<Enum>()
                .HaveConversion<string>()
                .HaveMaxLength(256);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(b =>
            {
                b.HasMany(m => m.Deposits)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId);

                b.HasMany(m => m.Commitments)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId);

                b.HasMany(m => m.CommitmentPeriods)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId);

                b.HasMany(m => m.Claims)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId)
                    .IsRequired();

                b.HasMany(m => m.Logins)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId)
                    .IsRequired();

                b.HasMany(m => m.Tokens)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId)
                    .IsRequired();

                b.HasMany(m => m.UserRoles)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId)
                    .IsRequired();
            });


            builder.Entity<Role>(b =>
            {
                b.HasMany(m => m.UserRoles)
                    .WithOne(m => m.Role)
                    .HasForeignKey(m => m.RoleId)
                    .IsRequired();

                b.HasMany(m => m.RoleClaims)
                    .WithOne(m => m.Role)
                    .HasForeignKey(m => m.RoleId)
                    .IsRequired();
            });


            builder.Entity<Deposit>(b =>
            {
                b.HasKey(m => m.Transaction);

                b.Property(m => m.Transaction)
                    .HasMaxLength(512);
            });


            builder.Entity<Metric>(b =>
            {
                b.HasKey(m => m.Name);

                b.Property(m => m.Name)
                    .HasMaxLength(256);

                b.HasMany(m => m.Values)
                    .WithOne(m => m.Metric)
                    .HasForeignKey(m => m.MetricName);

                b.HasData(
                    new Metric
                    {
                        Name = "Exercise"
                    },
                    new Metric
                    {
                        Name = "Running"
                    },
                    new Metric
                    {
                        Name = "Weight"
                    });
            });


            builder.Entity<MetricValue>(b =>
            {
                b.HasKey(m => new { m.MetricName, m.Type });

                b.HasMany(m => m.Providers)
                    .WithOne(m => m.MetricValue)
                    .HasForeignKey(m => new { m.MetricName, m.MetricType });

                b.HasData(
                    new MetricValue
                    {
                        MetricName = "Exercise",
                        Type = MetricType.Count
                    },
                    new MetricValue
                    {
                        MetricName = "Exercise",
                        Type = MetricType.Minutes
                    },
                    new MetricValue
                    {
                        MetricName = "Running",
                        Type = MetricType.Count
                    },
                    new MetricValue
                    {
                        MetricName = "Running",
                        Type = MetricType.Minutes
                    },
                    new MetricValue
                    {
                        MetricName = "Weight",
                        Type = MetricType.Value
                    });
            });


            builder.Entity<Provider>(b =>
            {
                b.HasKey(m => m.Name);

                b.Property(m => m.Name)
                    .HasMaxLength(256);

                b.HasMany(m => m.Logins)
                    .WithOne(m => m.Provider)
                    .HasForeignKey(m => m.LoginProvider);

                b.HasMany(m => m.Metrics)
                    .WithOne(m => m.Provider)
                    .HasForeignKey(m => m.ProviderName);

                b.HasData(
                    new Provider
                    {
                        Name = "Strava"
                    },
                    new Provider
                    {
                        Name = "Withings"
                    });
            });


            builder.Entity<ProviderMetricValue>(b =>
            {
                b.HasKey(m => new { m.ProviderName, m.MetricName, m.MetricType });

                b.HasMany(m => m.Goals)
                    .WithOne(m => m.Metric)
                    .HasForeignKey(m => new { m.ProviderName, m.MetricName, m.MetricType });

                b.HasData(
                    new ProviderMetricValue
                    {
                        ProviderName = "Strava",
                        MetricName = "Exercise",
                        MetricType = MetricType.Count
                    },
                    new ProviderMetricValue
                    {
                        ProviderName = "Strava",
                        MetricName = "Exercise",
                        MetricType = MetricType.Minutes
                    },
                    new ProviderMetricValue
                    {
                        ProviderName = "Strava",
                        MetricName = "Running",
                        MetricType = MetricType.Count
                    },
                    new ProviderMetricValue
                    {
                        ProviderName = "Strava",
                        MetricName = "Running",
                        MetricType = MetricType.Minutes
                    },
                    new ProviderMetricValue
                    {
                        ProviderName = "Withings",
                        MetricName = "Weight",
                        MetricType = MetricType.Value
                    });
            });

            builder.Entity<Commitment>(b =>
            {
                b.HasKey(m => m.Id);

                b.HasMany(m => m.Goals)
                    .WithOne(m => m.Commitment)
                    .HasForeignKey(m => m.CommitmentId);

                b.HasMany(m => m.Periods)
                    .WithOne(m => m.Commitment)
                    .HasForeignKey(m => m.CommitmentId);

                b.HasMany(m => m.Users)
                    .WithOne(m => m.Commitment)
                    .HasForeignKey(m => m.CommitmentId);
            });


            builder.Entity<Goal>(b =>
            {
                b.HasKey(m => new { m.CommitmentId, m.ProviderName, m.MetricName, m.MetricType });
            });


            builder.Entity<CommitmentPeriod>(b =>
            {
                b.HasKey(m => new { m.CommitmentId, m.StartDate, m.EndDate });

                b.HasMany(m => m.Users)
                    .WithOne(m => m.Commitment)
                    .HasForeignKey(m => new { m.CommitmentId, m.StartDate, m.EndDate });
            });


            builder.Entity<CommitmentUser>(b =>
            {
                b.HasKey(m => new { m.CommitmentId, m.UserId });
            });


            builder.Entity<CommitmentPeriodUser>(b =>
            {
                b.HasKey(m => new { m.CommitmentId, m.StartDate, m.EndDate, m.UserId });
            });
        }

    }
}
