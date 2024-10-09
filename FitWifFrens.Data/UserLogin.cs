using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    public class UserLogin : IdentityUserLogin<string>
    {
        public User User { get; set; }
    }
}
