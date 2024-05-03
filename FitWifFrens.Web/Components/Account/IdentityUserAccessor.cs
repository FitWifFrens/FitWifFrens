using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Web.Components.Account
{
    internal sealed class IdentityUserAccessor(UserManager<User> userManager, IdentityRedirectManager redirectManager)
    {
        public Task<User?> GetUserAsync(HttpContext context)
        {
            return userManager.GetUserAsync(context.User);
        }

        public async Task<User> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null)
            {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            }

            return user;
        }
    }
}
