using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Web.Background
{
    public class NotificationService
    {
        private readonly NotificationServiceConfiguration _notificationServiceConfiguration;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(NotificationServiceConfiguration notificationServiceConfiguration, IServiceScopeFactory serviceScopeFactory, IHttpClientFactory httpClientFactory, TelemetryClient telemetryClient, ILogger<NotificationService> logger)
        {
            _notificationServiceConfiguration = notificationServiceConfiguration;
            _serviceScopeFactory = serviceScopeFactory;
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

            await SaveBotMessageAsync(_notificationServiceConfiguration.ChatId, message);
        }

        private async Task SaveBotMessageAsync(string chatId, string text)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var chat = await dataContext.Chats.FindAsync(chatId);
                if (chat == null)
                {
                    dataContext.Chats.Add(new Chat
                    {
                        ChatId = chatId,
                        CreatedTime = DateTime.UtcNow
                    });
                    await dataContext.SaveChangesAsync();
                }

                dataContext.ChatMessages.Add(new ChatMessage
                {
                    ChatId = chatId,
                    TelegramUserId = 0,
                    DisplayName = "Bot",
                    Text = text.Length > 4096 ? text[..4096] : text,
                    Timestamp = DateTime.UtcNow
                });

                await dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save bot message. ChatId={ChatId}", chatId);
            }
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
