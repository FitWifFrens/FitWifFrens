using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Data
{
    // Add profile data for application users by adding properties to the User class
    public class User : IdentityUser
    {
        [ProtectedPersonalData]
        public override string? UserName { get; set; }

        [ProtectedPersonalData]
        public override string? Email { get; set; }

        public ICollection<UserClaim> Claims { get; set; }
        public ICollection<UserLogin> Logins { get; set; }
        public ICollection<UserToken> Tokens { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }

}
