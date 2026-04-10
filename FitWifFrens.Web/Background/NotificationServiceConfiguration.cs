namespace FitWifFrens.Web.Background
{
    public class NotificationServiceConfiguration
    {
        public string Token { get; init; } = string.Empty;
        public string ChatId { get; init; } = string.Empty;
        public string? WebhookSecretToken { get; init; }
        public string? BotUsername { get; init; }
    }
}
