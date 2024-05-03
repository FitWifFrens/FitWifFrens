using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    // Add profile data for application users by adding properties to the User class
    public class User : IdentityUser
    {
        public ICollection<CommittedUser> Commitments { get; set; }

        public ICollection<UserClaim> Claims { get; set; }
        public ICollection<UserLogin> Logins { get; set; }
        public ICollection<UserToken> Tokens { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }

}
