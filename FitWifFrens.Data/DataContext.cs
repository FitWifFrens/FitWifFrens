using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Data
{
    public class DataContext : IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Metric> Metrics { get; set; }
        public DbSet<MetricValue> MetricValues { get; set; }
        public DbSet<MetricProvider> MetricProviders { get; set; }
        public DbSet<UserMetricProvider> UserMetricProviders { get; set; }
        public DbSet<UserMetricProviderValue> UserMetricProviderValues { get; set; }
        public DbSet<Commitment> Commitments { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<CommitmentPeriod> CommitmentPeriods { get; set; }
        public DbSet<CommitmentUser> CommitmentUsers { get; set; }
        public DbSet<CommitmentPeriodUser> CommitmentPeriodUsers { get; set; }
        public DbSet<CommitmentPeriodUserGoal> CommitmentPeriodUserGoals { get; set; }
        public DbSet<Display> Displays { get; set; }
        public DbSet<UserDisplay> UserDisplays { get; set; }

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

                b.HasMany(m => m.MetricProviders)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId);

                b.HasMany(m => m.MetricProviderValues)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId);

                b.HasMany(m => m.Commitments)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId);

                b.HasMany(m => m.CommitmentPeriods)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId);

                b.HasMany(m => m.Displays)
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

                b.HasMany(m => m.Providers)
                    .WithOne(m => m.Metric)
                    .HasForeignKey(m => m.MetricName);

                b.HasMany(m => m.Values)
                    .WithOne(m => m.Metric)
                    .HasForeignKey(m => m.MetricName);
            });


            builder.Entity<MetricValue>(b =>
            {
                b.HasKey(m => new { m.MetricName, m.Type });

                b.HasMany(m => m.Goals)
                    .WithOne(m => m.MetricValue)
                    .HasForeignKey(m => new { m.MetricName, m.MetricType });

                b.HasMany(m => m.Values)
                    .WithOne(m => m.MetricValue)
                    .HasForeignKey(m => new { m.MetricName, m.MetricType });
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
            });


            builder.Entity<MetricProvider>(b =>
            {
                b.HasKey(m => new { m.MetricName, m.ProviderName });

                b.HasMany(m => m.Users)
                    .WithOne(m => m.MetricProvider)
                    .HasForeignKey(m => new { m.MetricName, m.ProviderName });

                b.HasMany(m => m.Values)
                    .WithOne(m => m.MetricProvider)
                    .HasForeignKey(m => new { m.MetricName, m.ProviderName });

                b.HasMany(m => m.Goals)
                    .WithOne(m => m.MetricProvider)
                    .HasForeignKey(m => new { m.MetricName, m.ProviderName });
            });

            builder.Entity<UserMetricProvider>(b =>
            {
                b.HasKey(m => new { m.UserId, m.MetricName });
            });

            builder.Entity<UserMetricProviderValue>(b =>
            {
                b.HasKey(m => new { m.UserId, m.MetricName, m.ProviderName, m.MetricType, m.Time });
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
                b.HasKey(m => new { m.CommitmentId, m.MetricName, m.MetricType });

                b.HasMany(m => m.Users)
                    .WithOne(m => m.Goal)
                    .HasForeignKey(m => new { m.CommitmentId, m.MetricName, m.MetricType });
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

                b.HasMany(m => m.Goals)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => new { m.CommitmentId, m.StartDate, m.EndDate, m.UserId });
            });

            builder.Entity<CommitmentPeriodUserGoal>(b =>
            {
                b.HasKey(m => new { m.CommitmentId, m.StartDate, m.EndDate, m.UserId, m.MetricName, m.MetricType, m.ProviderName });
            });


            builder.Entity<Display>(b =>
            {
                b.HasKey(m => new { m.MacAddress });

                b.Property(m => m.MacAddress)
                    .HasMaxLength(17)
                    .IsFixedLength();

                b.HasOne(m => m.User)
                    .WithOne(m => m.Display)
                    .HasForeignKey<UserDisplay>(m => m.MacAddress);
            });


            builder.Entity<UserDisplay>(b =>
            {
                b.HasKey(m => new { m.UserId, m.MacAddress });
            });
        }

    }
}
