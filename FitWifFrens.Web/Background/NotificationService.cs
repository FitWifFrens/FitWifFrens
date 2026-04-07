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

        public string ChatId => _notificationServiceConfiguration.ChatId;

        public async Task Notify(string message)
        {
            await _httpClient.PostAsJsonAsync($"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/sendMessage", new
            {
                chat_id = _notificationServiceConfiguration.ChatId,
                text = message
            });
        }

        public async Task NotifyWithPhoto(Stream imageStream, string? caption = null)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(_notificationServiceConfiguration.ChatId), "chat_id");
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(streamContent, "photo", "chart.png");
            if (!string.IsNullOrEmpty(caption))
                content.Add(new StringContent(caption), "caption");
            await _httpClient.PostAsync($"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/sendPhoto", content);
        }
    }
}
