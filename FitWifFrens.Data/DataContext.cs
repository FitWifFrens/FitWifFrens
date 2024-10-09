using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Data
{
    public class DataContext : IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
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
        }

    }
}
