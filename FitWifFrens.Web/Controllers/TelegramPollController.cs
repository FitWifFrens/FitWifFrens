using FitWifFrens.Web.Telegram;
using Microsoft.AspNetCore.Mvc;

namespace FitWifFrens.Web.Controllers
{
    public sealed record SendTelegramPollRequest(
        string Question,
        IReadOnlyCollection<string> Options,
        string? ChatId,
        bool AllowsMultipleAnswers = false,
        bool IsAnonymous = false);

    public sealed record RegisterTelegramWebhookRequest(string WebhookUrl, string? SecretToken);

    [ApiController]
    [Route("api/telegram/polls")]
    public class TelegramPollController : ControllerBase
    {
        private readonly TelegramPollService _telegramPollService;

        public TelegramPollController(TelegramPollService telegramPollService)
        {
            _telegramPollService = telegramPollService;
        }

        [HttpPost]
        public async Task<IActionResult> SendPoll([FromBody] SendTelegramPollRequest request, CancellationToken cancellationToken)
        {
            var result = await _telegramPollService.SendPollAsync(
                request.Question,
                request.Options,
                request.ChatId,
                request.AllowsMultipleAnswers,
                request.IsAnonymous,
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("{pollId}/responses")]
        public IActionResult GetResponses(string pollId)
        {
            return Ok(_telegramPollService.GetResponses(pollId));
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> RegisterWebhook([FromBody] RegisterTelegramWebhookRequest request, CancellationToken cancellationToken)
        {
            await _telegramPollService.SetWebhookAsync(request.WebhookUrl, request.SecretToken, cancellationToken);
            return Ok();
        }
    }
}
