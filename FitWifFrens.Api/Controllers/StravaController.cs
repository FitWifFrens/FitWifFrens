using FitWifFrens.Api.Services;
using FitWifFrens.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitWifFrens.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("strava")]
    public class StravaController : ControllerBase
    {
        private readonly StravaService _stravaService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        public StravaController(StravaService stravaService, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _stravaService = stravaService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet("isAuthenticated")]
        public IActionResult IsAuthenticated()
        {
            // Check if the user is authenticated
            if (User.Identity is { IsAuthenticated: true })
            {
                return Ok(new
                {
                    IsAuthenticated = true,
                    UserName = User.Identity.Name
                });
            }

            return Unauthorized(new
            {
                IsAuthenticated = false,
                Message = "User is not authenticated."
            });
        }

        [HttpGet("connect")]
        public IActionResult ConnectStrava()
        {
            var redirectUri = Url.Action("StravaCallback", "Strava");
            var properties = new AuthenticationProperties { RedirectUri = redirectUri };

            return Challenge(properties, "Strava");
        }

        [HttpGet("strava-callback")]
        public async Task<IActionResult> StravaCallback()
        {
            var result = await HttpContext.AuthenticateAsync("Strava");
            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            var accessToken = result.Properties.GetTokenValue("access_token");
            var refreshToken = result.Properties.GetTokenValue("refresh_token");
            var expiresAt = result.Properties.GetTokenValue("expires_at");

            if (accessToken == null || refreshToken == null || expiresAt == null)
            {
                return BadRequest("Failed to retrieve Strava tokens.");
            }

            var user = await _userManager.FindByEmailAsync(User.Identity.Name);
            if (user == null) return BadRequest("User is null");
            await _stravaService.SaveTokensAsync(user.Id, "Strava", accessToken, refreshToken,
                DateTime.Parse(expiresAt));
            return Ok("Strava connected and tokens saved successfully.");

        }
    }
}
