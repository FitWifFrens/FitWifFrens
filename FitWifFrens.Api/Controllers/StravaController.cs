using FitWifFrens.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitWifFrens.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("strava")]
    public class StravaController : ControllerBase
    {
        private readonly StravaService _stravaService;
        public StravaController(StravaService stravaService)
        {
            _stravaService = stravaService;
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

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("User is not authenticated.");
            }

            await _stravaService.SaveTokensAsync(userId, "Strava", accessToken, refreshToken,
                DateTime.Parse(expiresAt));
            return Ok("Strava connected and tokens saved successfully.");

        }
    }
}
