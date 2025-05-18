using Microsoft.ApplicationInsights;

namespace FitWifFrens.Web.Background
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IHttpClientFactory httpClientFactory, TelemetryClient telemetryClient, ILogger<NotificationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task Notify(string message)
        {
            await _httpClient.PostAsJsonAsync("https://api.telegram.org/bot8189809938:AAE5ooQCxlhKQgYynabpeFysvwoVNwMlQEY/sendMessage", new
            {
                chat_id = "-4626525805",
                text = message
            });
        }
    }
}
