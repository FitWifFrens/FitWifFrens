using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    public class UserRole : IdentityUserRole<string>
    {
        public User User { get; set; }
        public Role Role { get; set; }
    }
}
