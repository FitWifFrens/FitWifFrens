using FitWifFrens.Web.Background;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FitWifFrens.Web.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : Controller
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public WebhooksController(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpGet("strava")]
        public IActionResult ConfirmStrava([FromQuery(Name = "hub.challenge")] string challenge)
        {
            return Json(new JsonObject
            {
                { "hub.challenge", challenge }
            });
        }

        [HttpPost("strava")]
        public IActionResult UpdateStrava([FromBody] JsonElement dataJson)
        {
            _backgroundJobClient.Enqueue<StravaService>(s => s.UpdateProviderMetricValues(CancellationToken.None));

            return Ok();
        }

        [HttpPost("withings")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult UpdateWithings([FromQuery] string userId, [FromForm] IFormCollection dataFrom)
        {
            if (dataFrom.Any())
            {
                _backgroundJobClient.Enqueue<WithingsService>(s => s.UpdateProviderMetricValues(CancellationToken.None));
            }

            return Ok();
        }
    }
}
