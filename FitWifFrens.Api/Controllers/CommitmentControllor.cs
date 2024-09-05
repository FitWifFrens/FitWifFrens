using FitWifFrens.Api.Dtos;
using FitWifFrens.Api.Mappers;
using FitWifFrens.Api.Services;
using FitWifFrens.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<CommitmentDto>))]
        public async Task<IActionResult> GetAllCommitments()
        {
            var commitments = await _dataContext.Commitments.ToListAsync();

            return Ok(commitments.Select(c => c.ToCommitmentDto()));
        }

        [HttpGet("strava")]
        public async Task<IActionResult> GetStravaUserActivities(CancellationToken cancellationToken)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Unauthorized("Access token is missing");
            }

            await _stravaService.GetUserActivitiesAsync(accessToken, cancellationToken);
            return Ok();
        }
    }
}
