using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    public class RoleClaim : IdentityRoleClaim<string>
    {
        public Role Role { get; set; }
    }
}
