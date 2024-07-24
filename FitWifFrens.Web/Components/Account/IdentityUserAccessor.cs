using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;

namespace FitWifFrens.Web.Components.Account
{
    public sealed class IdentityUserAccessor(UserManager<User> userManager, IdentityRedirectManager redirectManager)
    {
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        public async Task<User?> GetUserAsync(HttpContext context)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                return await userManager.GetUserAsync(context.User);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<User> GetRequiredUserAsync(HttpContext context)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user is null)
                {
                    redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
                }

                return user;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
