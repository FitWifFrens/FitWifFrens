using FitWifFrens.Web.Background;
using Hangfire;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FitWifFrens.Web.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : Controller
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly TelemetryClient _telemetryClient;

        public WebhooksController(IBackgroundJobClient backgroundJobClient, TelemetryClient telemetryClient)
        {
            _backgroundJobClient = backgroundJobClient;
            _telemetryClient = telemetryClient;
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
            _telemetryClient.TrackTrace("UpdateStrava ~ " + dataJson.GetRawText());

            if (dataJson.TryGetProperty("owner_id", out var stravaIdJson))
            {
                _backgroundJobClient.Enqueue<StravaService>(s => s.UpdateProviderMetricValues(stravaIdJson.GetInt32().ToString(), CancellationToken.None));
            }

            return Ok();
        }

        [HttpPost("withings")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult UpdateWithings([FromQuery] string userId, [FromForm] IFormCollection dataFrom)
        {
            _telemetryClient.TrackTrace("UpdateWithings ~ " + string.Join(", ", dataFrom.Select(k => $"{k.Key}={k.Value.First()}")));

            if (dataFrom.TryGetValue("userid", out var withingsIdValues))
            {
                _backgroundJobClient.Enqueue<WithingsService>(s => s.UpdateProviderMetricValues(withingsIdValues.Single()!, CancellationToken.None)); // TODO: and then update goals
            }

            return Ok();
        }
    }
}
