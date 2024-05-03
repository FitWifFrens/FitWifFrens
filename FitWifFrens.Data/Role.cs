using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    public class Role : IdentityRole
    {
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<RoleClaim> RoleClaims { get; set; }
    }
}
