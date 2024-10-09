using FitWifFrens.Api.Services;
using FitWifFrens.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitWifFrens.Api.Controllers
{
    [ApiController]
    [Route("commitments")]
    public class CommitmentControllor : ControllerBase
    {
        private readonly DataContext _dataContext;
        private readonly StravaService _stravaService;

        public CommitmentControllor(DataContext dataContext, StravaService stravaService)
        {
            _dataContext = dataContext;
            _stravaService = stravaService;
        }

        [HttpGet("strava")]
        public async Task<IActionResult> GetStravaUserActivities(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("User is not authenticated.");
            }

            var accessToken = await _dataContext.UserTokens
                .Where(u => u.UserId == userId && u.Name == "access_token")
                .Select(u => u.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Unauthorized("Access token is missing");
            }

            await _stravaService.GetUserActivitiesAsync(accessToken, cancellationToken);
            return Ok();
        }
    }
}
