using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    public class UserToken : IdentityUserToken<string>
    {
        public User User { get; set; }
    }
}
