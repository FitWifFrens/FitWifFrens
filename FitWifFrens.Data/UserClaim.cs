using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    public class UserClaim : IdentityUserClaim<string>
    {
        public User User { get; set; }
    }
}
