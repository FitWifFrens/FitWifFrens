using FitWifFrens.Web.Background;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("withings")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult Get([FromQuery] string userId, [FromForm] IFormCollection data)
        {
            if (data.Any())
            {
                _backgroundJobClient.Enqueue<WithingsService>(s => s.UpdateProviderMetricValues(CancellationToken.None));
            }

            return Ok();
        }
    }
}
