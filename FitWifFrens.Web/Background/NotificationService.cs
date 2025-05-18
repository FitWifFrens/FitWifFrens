using Microsoft.ApplicationInsights;

namespace FitWifFrens.Web.Background
{
    public class NotificationService
    {
        private readonly NotificationServiceConfiguration _notificationServiceConfiguration;
        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(NotificationServiceConfiguration notificationServiceConfiguration, IHttpClientFactory httpClientFactory, TelemetryClient telemetryClient, ILogger<NotificationService> logger)
        {
            _notificationServiceConfiguration = notificationServiceConfiguration;
            _httpClient = httpClientFactory.CreateClient();
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task Notify(string message)
        {
            await _httpClient.PostAsJsonAsync($"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/sendMessage", new
            {
                chat_id = _notificationServiceConfiguration.ChatId,
                text = message
            });
        }
    }
}
