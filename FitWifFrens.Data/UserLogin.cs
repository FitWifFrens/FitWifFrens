using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    public class UserLogin : IdentityUserLogin<string>
    {
        public Provider Provider { get; set; }

        public User User { get; set; }
    }
}
