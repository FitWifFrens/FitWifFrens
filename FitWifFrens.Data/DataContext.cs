using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Data
{
    public class
        DataContext : IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Commitment> Commitments { get; set; }
        public DbSet<CommitmentProvider> CommitmentProviders { get; set; }
        public DbSet<CommittedUser> CommittedUsers { get; set; }


        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(b =>
            {
                b.HasMany(m => m.Commitments)
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

            builder.Entity<Provider>(b =>
            {
                b.HasKey(m => m.Name);

                b.Property(m => m.Name)
                    .HasMaxLength(450);

                b.HasMany(m => m.Logins)
                    .WithOne(m => m.Provider)
                    .HasForeignKey(m => m.LoginProvider);

                b.HasMany(m => m.Commitments)
                    .WithOne(m => m.Provider)
                    .HasForeignKey(m => m.ProviderName);

                b.HasData(
                    new Provider
                    {
                        Name = "WorldId"
                    },
                    new Provider
                    {
                        Name = "StackExchange"
                    },
                    new Provider
                    {
                        Name = "Strava"
                    });
            });

            builder.Entity<Commitment>(b =>
            {
                b.HasKey(m => m.Id);

                b.HasMany(m => m.Providers)
                    .WithOne(m => m.Commitment)
                    .HasForeignKey(m => m.CommitmentId);

                b.HasMany(m => m.Users)
                    .WithOne(m => m.Commitment)
                    .HasForeignKey(m => m.CommitmentId);
            });


            builder.Entity<CommitmentProvider>(b =>
            {
                b.HasKey(m => new { m.CommitmentId, m.ProviderName });
            });


            builder.Entity<CommittedUser>(b =>
            {
                b.HasKey(m => new { m.CommitmentId, m.UserId });
            });
        }

    }
}
