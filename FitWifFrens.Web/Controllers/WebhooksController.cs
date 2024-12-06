using FitWifFrens.Web.Background;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FitWifFrens.Web.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : Controller
    {
        private readonly IScheduler _scheduler;
        private readonly TelemetryClient _telemetryClient;

        public WebhooksController(IScheduler scheduler, TelemetryClient telemetryClient)
        {
            _scheduler = scheduler;
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
                var jobDataMap = new JobDataMap(1);
                jobDataMap.Put("StravaId", stravaIdJson.GetInt32().ToString());
                _scheduler.TriggerJob(StravaJob.JobKey, jobDataMap);
            }

            return Ok();
        }

        [HttpPost("withings")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult UpdateWithings([FromForm] IFormCollection dataFrom)
        {
            _telemetryClient.TrackTrace("UpdateWithings ~ " + string.Join(", ", dataFrom.Select(k => $"{k.Key}={k.Value.First()}")));

            if (dataFrom.TryGetValue("userid", out var withingsIdValues))
            {
                var jobDataMap = new JobDataMap(1);
                jobDataMap.Put("WithingsId", withingsIdValues.Single()!);
                _scheduler.TriggerJob(WithingsJob.JobKey, jobDataMap);
            }

            return Ok();
        }
    }
}
