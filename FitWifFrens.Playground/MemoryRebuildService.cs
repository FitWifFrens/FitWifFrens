using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK;
using FitWifFrens.Data;
using FitWifFrens.Web; // Constants
using FitWifFrens.Web.Background; // AiSummaryService
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Playground
{
    /// <summary>
    /// One-off recovery utility. Imports a Telegram chat-history export into the ChatMessages
    /// table, then rebuilds the bot's long-term memory day by day from a known good point.
    ///
    /// Usage:
    /// 1. Manually seed the BotMemory row for the chat with the last good Summary and set its
    ///    UpdatedTime to the day you want to rebuild forward from.
    /// 2. Export the chat history from Telegram Desktop (Export chat history -> JSON -> result.json).
    /// 3. Point MemoryRebuild:ExportFilePath (config) or DefaultExportFilePath (below) at result.json.
    /// 4. Enable this service in Program.cs (and disable RecreateService) and run the Playground.
    ///
    /// The rebuild loads BotMemory.UpdatedTime as the starting point and walks one day at a time up
    /// to now, feeding each day's messages plus the running summary into AiSummaryService.ExtractMemories
    /// and saving the result back to the database after every day (with UpdatedTime advanced to that
    /// day's boundary, so a re-run resumes where it left off).
    /// </summary>
    public class MemoryRebuildService : IHostedService
    {
        // ===== Edit these before running =====

        /// <summary>Import the Telegram export JSON into ChatMessages before rebuilding.</summary>
        private const bool DoImport = true;

        /// <summary>Rebuild the memory summary day by day.</summary>
        private const bool DoRebuild = true;

        /// <summary>
        /// Path to the Telegram Desktop export (result.json). Overridable via the config key
        /// "MemoryRebuild:ExportFilePath".
        /// </summary>
        private const string DefaultExportFilePath = "result.json";

        // =====================================

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MemoryRebuildService> _logger;

        public MemoryRebuildService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<MemoryRebuildService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // The chat to rebuild — defaults to the bot's configured chat so imported messages and the
            // seeded BotMemory line up with how the live bot stores data.
            var chatId = _configuration.GetValue<string>("Services:Telegram:ChatId");
            if (string.IsNullOrWhiteSpace(chatId))
            {
                throw new InvalidOperationException("Services:Telegram:ChatId is not configured. Set it (user secrets / appsettings) before running the memory rebuild.");
            }

            if (DoImport)
            {
                var exportFilePath = _configuration.GetValue<string>("MemoryRebuild:ExportFilePath") ?? DefaultExportFilePath;
                await ImportTelegramExportAsync(chatId, exportFilePath, cancellationToken);
            }

            if (DoRebuild)
            {
                await RebuildMemoryAsync(chatId, cancellationToken);
            }

            _logger.LogInformation("MemoryRebuildService finished.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task ImportTelegramExportAsync(string chatId, string exportFilePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(exportFilePath))
            {
                throw new FileNotFoundException(
                    $"Telegram export not found at '{Path.GetFullPath(exportFilePath)}'. Set MemoryRebuild:ExportFilePath or place result.json in the working directory.",
                    exportFilePath);
            }

            _logger.LogInformation("Importing Telegram export from {Path} into ChatId={ChatId}", exportFilePath, chatId);

            TelegramExport? export;
            await using (var stream = File.OpenRead(exportFilePath))
            {
                export = await JsonSerializer.DeserializeAsync<TelegramExport>(stream, cancellationToken: cancellationToken);
            }

            if (export?.Messages == null || export.Messages.Count == 0)
            {
                _logger.LogWarning("Export contained no messages, skipping import.");
                return;
            }

            var toInsert = new List<ChatMessage>();
            foreach (var m in export.Messages)
            {
                if (!string.Equals(m.Type, "message", StringComparison.OrdinalIgnoreCase)) continue; // skip service messages (joins, pins, etc.)
                if (string.IsNullOrWhiteSpace(m.From)) continue;

                var text = FlattenText(m.Text);
                if (string.IsNullOrWhiteSpace(text)) continue; // media-only / empty messages add nothing to memory

                var timestamp = ParseTimestamp(m);
                if (timestamp == null) continue;

                toInsert.Add(new ChatMessage
                {
                    ChatId = chatId,
                    TelegramUserId = ParseUserId(m.FromId),
                    DisplayName = m.From!.Length > 256 ? m.From![..256] : m.From!,
                    Text = text.Length > 4096 ? text[..4096] : text,
                    Timestamp = timestamp.Value
                });
            }

            if (toInsert.Count == 0)
            {
                _logger.LogWarning("No importable messages found in export, skipping.");
                return;
            }

            var minTs = toInsert.Min(x => x.Timestamp);
            var maxTs = toInsert.Max(x => x.Timestamp);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await EnsureChatExistsAsync(dataContext, chatId, export.Name, cancellationToken);

            // Make the import idempotent: clear any existing messages for this chat that fall within the
            // export's date range before inserting, so re-running doesn't create duplicates.
            var existing = await dataContext.ChatMessages
                .Where(x => x.ChatId == chatId && x.Timestamp >= minTs && x.Timestamp <= maxTs)
                .ToListAsync(cancellationToken);
            if (existing.Count > 0)
            {
                _logger.LogInformation("Removing {Count} existing messages in the import range before re-import.", existing.Count);
                dataContext.ChatMessages.RemoveRange(existing);
                await dataContext.SaveChangesAsync(cancellationToken);
            }

            dataContext.ChatMessages.AddRange(toInsert);
            await dataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Imported {Count} messages spanning {Min:yyyy-MM-dd} to {Max:yyyy-MM-dd}.", toInsert.Count, minTs, maxTs);
        }

        private async Task RebuildMemoryAsync(string chatId, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var aiSummaryService = scope.ServiceProvider.GetRequiredService<AiSummaryService>();

            // Fail loudly rather than silently advancing UpdatedTime with no summary changes.
            if (scope.ServiceProvider.GetService<AnthropicClient>() == null)
            {
                throw new InvalidOperationException("Services:Anthropic:ApiKey is not configured — cannot rebuild memory without the Anthropic client.");
            }

            var memory = await dataContext.BotMemories.SingleOrDefaultAsync(m => m.ChatId == chatId, cancellationToken);
            if (memory == null)
            {
                throw new InvalidOperationException(
                    $"No BotMemory row for ChatId={chatId}. Seed it first with the last good Summary and set UpdatedTime to the point you want to rebuild from.");
            }

            var soulPrompt = await AiSummaryService.LoadSoulPromptAsync(dataContext, chatId, cancellationToken);

            var now = DateTime.UtcNow;
            var startPoint = DateTime.SpecifyKind(memory.UpdatedTime, DateTimeKind.Utc);
            var summary = memory.Summary;

            _logger.LogInformation("Rebuilding memory for ChatId={ChatId} from {Start:yyyy-MM-dd HH:mm} to now.", chatId, startPoint);

            // Walk one calendar day (UTC) at a time, starting from the known good point.
            var day = new DateTime(startPoint.Year, startPoint.Month, startPoint.Day, 0, 0, 0, DateTimeKind.Utc);
            while (day < now)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var windowStart = day < startPoint ? startPoint : day; // don't reprocess before the known point on the first day
                var nextDay = day.AddDays(1);
                var windowEnd = nextDay < now ? nextDay : now;         // last (partial) day ends at now

                var messages = await dataContext.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.ChatId == chatId && m.Timestamp >= windowStart && m.Timestamp < windowEnd)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync(cancellationToken);

                if (messages.Count > 0)
                {
                    var updated = await aiSummaryService.ExtractMemories(
                        messages.Select(m => (m.DisplayName, m.Text, m.Timestamp)),
                        summary,
                        cancellationToken,
                        soulPrompt);

                    if (!string.IsNullOrWhiteSpace(updated))
                    {
                        summary = updated.Length > Constants.Memory.MaxSummaryLength ? updated[..Constants.Memory.MaxSummaryLength] : updated;
                        memory.Summary = summary;
                    }

                    _logger.LogInformation("Processed {Date:yyyy-MM-dd}: {Count} messages.", day, messages.Count);
                }
                else
                {
                    _logger.LogInformation("Processed {Date:yyyy-MM-dd}: no messages.", day);
                }

                // Persist progress after every day so the run is resumable.
                memory.UpdatedTime = windowEnd;
                await dataContext.SaveChangesAsync(cancellationToken);

                day = nextDay;
            }

            _logger.LogInformation("Memory rebuild complete for ChatId={ChatId}. Final summary length={Length} chars.", chatId, summary.Length);
        }

        private static async Task EnsureChatExistsAsync(DataContext dataContext, string chatId, string? title, CancellationToken cancellationToken)
        {
            var chat = await dataContext.Chats.FindAsync(new object[] { chatId }, cancellationToken);
            if (chat == null)
            {
                dataContext.Chats.Add(new Chat
                {
                    ChatId = chatId,
                    Title = title,
                    CreatedTime = DateTime.UtcNow
                });
                await dataContext.SaveChangesAsync(cancellationToken);
            }
        }

        private static long ParseUserId(string? fromId)
        {
            // Telegram from_id looks like "user123456789" or "channel123456789"; keep just the digits.
            if (string.IsNullOrEmpty(fromId)) return 0;
            var digits = new string(fromId.Where(char.IsDigit).ToArray());
            return long.TryParse(digits, out var id) ? id : 0;
        }

        private static DateTime? ParseTimestamp(TelegramExportMessage m)
        {
            // Prefer date_unixtime (UTC epoch seconds); fall back to the (local) date string.
            if (!string.IsNullOrEmpty(m.DateUnixtime) && long.TryParse(m.DateUnixtime, out var unix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            }

            if (!string.IsNullOrEmpty(m.Date) &&
                DateTime.TryParse(m.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            {
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }

            return null;
        }

        private static string FlattenText(JsonElement text)
        {
            // Telegram "text" is either a plain string or an array of (string | { "type", "text" }).
            switch (text.ValueKind)
            {
                case JsonValueKind.String:
                    return text.GetString() ?? string.Empty;

                case JsonValueKind.Array:
                    var sb = new StringBuilder();
                    foreach (var part in text.EnumerateArray())
                    {
                        if (part.ValueKind == JsonValueKind.String)
                        {
                            sb.Append(part.GetString());
                        }
                        else if (part.ValueKind == JsonValueKind.Object &&
                                 part.TryGetProperty("text", out var inner) &&
                                 inner.ValueKind == JsonValueKind.String)
                        {
                            sb.Append(inner.GetString());
                        }
                    }
                    return sb.ToString();

                default:
                    return string.Empty;
            }
        }

        private sealed class TelegramExport
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("messages")] public List<TelegramExportMessage> Messages { get; set; } = new();
        }

        private sealed class TelegramExportMessage
        {
            [JsonPropertyName("type")] public string? Type { get; set; }
            [JsonPropertyName("date")] public string? Date { get; set; }
            [JsonPropertyName("date_unixtime")] public string? DateUnixtime { get; set; }
            [JsonPropertyName("from")] public string? From { get; set; }
            [JsonPropertyName("from_id")] public string? FromId { get; set; }
            [JsonPropertyName("text")] public JsonElement Text { get; set; }
        }
    }
}
