using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.SDK;
using FitWifFrens.Web; // Constants
using FitWifFrens.Web.Background; // AiSummaryService

namespace FitWifFrens.Playground
{
    /// <summary>
    /// One-off, file-based memory rebuild utility. Reads a starting memory summary from a local file
    /// and a Telegram chat-history export, then rebuilds the memory day by day from a given start date,
    /// writing the result to a local output file. It never touches the database — review the output and
    /// paste it into BotMemory.Summary yourself.
    ///
    /// Configuration (user secrets / appsettings / command line):
    ///   Services:Anthropic:ApiKey          - Anthropic API key (already used by the web app)
    ///   MemoryRebuild:ExportFilePath       - Telegram Desktop export (result.json) with the chat history
    ///   MemoryRebuild:StartDate            - date to start rebuilding from, e.g. 2026-05-01 (UTC, inclusive)
    ///   MemoryRebuild:InputMemoryFilePath  - optional file with the last good memory to start from
    ///   MemoryRebuild:OutputMemoryFilePath - where to write the rebuilt memory (default: memory-rebuilt.md)
    ///   MemoryRebuild:SoulFilePath         - optional file whose contents are used as the bot "soul" system prompt
    ///
    /// The rebuilt memory is written to the output file after every day, so you can watch it grow and
    /// keep progress if the run is interrupted.
    /// </summary>
    public class MemoryRebuildService : IHostedService
    {
        private const string DefaultOutputMemoryFilePath = "memory-rebuilt.md";

        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<MemoryRebuildService> _logger;

        public MemoryRebuildService(IConfiguration configuration, ILoggerFactory loggerFactory, ILogger<MemoryRebuildService> logger)
        {
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var apiKey = _configuration.GetValue<string>("Services:Anthropic:ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Services:Anthropic:ApiKey is not configured.");
            }

            var exportFilePath = _configuration.GetValue<string>("MemoryRebuild:ExportFilePath");
            if (string.IsNullOrWhiteSpace(exportFilePath))
            {
                throw new InvalidOperationException("MemoryRebuild:ExportFilePath is not configured (path to the Telegram export result.json).");
            }
            if (!File.Exists(exportFilePath))
            {
                throw new FileNotFoundException($"Telegram export not found at '{Path.GetFullPath(exportFilePath)}'.", exportFilePath);
            }

            var startDateText = _configuration.GetValue<string>("MemoryRebuild:StartDate");
            if (string.IsNullOrWhiteSpace(startDateText) ||
                !DateOnly.TryParse(startDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            {
                throw new InvalidOperationException("MemoryRebuild:StartDate is not configured or invalid (expected a date like 2026-05-01, interpreted as UTC).");
            }

            var inputMemoryFilePath = _configuration.GetValue<string>("MemoryRebuild:InputMemoryFilePath");
            var outputMemoryFilePath = _configuration.GetValue<string>("MemoryRebuild:OutputMemoryFilePath") ?? DefaultOutputMemoryFilePath;
            var soulFilePath = _configuration.GetValue<string>("MemoryRebuild:SoulFilePath");

            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputMemoryFilePath));
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Starting memory (optional — omit to rebuild from scratch).
            var summary = string.Empty;
            if (!string.IsNullOrWhiteSpace(inputMemoryFilePath))
            {
                if (!File.Exists(inputMemoryFilePath))
                {
                    throw new FileNotFoundException($"Input memory file not found at '{Path.GetFullPath(inputMemoryFilePath)}'.", inputMemoryFilePath);
                }
                summary = await File.ReadAllTextAsync(inputMemoryFilePath, cancellationToken);
                _logger.LogInformation("Loaded starting memory ({Length} chars) from {Path}.", summary.Length, inputMemoryFilePath);
            }
            else
            {
                _logger.LogInformation("No input memory file configured — starting from an empty summary.");
            }

            // Optional bot personality, used as the extraction system message (matches the live bot).
            string? soulPrompt = null;
            if (!string.IsNullOrWhiteSpace(soulFilePath) && File.Exists(soulFilePath))
            {
                soulPrompt = await File.ReadAllTextAsync(soulFilePath, cancellationToken);
            }

            var messages = await LoadExportMessagesAsync(exportFilePath, cancellationToken);
            if (messages.Count == 0)
            {
                _logger.LogWarning("No usable messages found in the export, nothing to rebuild.");
                return;
            }

            var byDay = messages.ToLookup(m => DateOnly.FromDateTime(m.Timestamp));
            var lastMessageDay = DateOnly.FromDateTime(messages[^1].Timestamp);

            // Reuse the live extraction logic (same prompt, token limit and self-capping behaviour).
            var aiSummaryService = new AiSummaryService(new AnthropicClient(apiKey, new HttpClient { Timeout = TimeSpan.FromSeconds(600) }), _loggerFactory.CreateLogger<AiSummaryService>());

            _logger.LogInformation("Rebuilding memory from {Start} to {End} ({Count} messages). Output: {Output}",
                startDate, lastMessageDay, messages.Count, Path.GetFullPath(outputMemoryFilePath));

            for (var day = startDate; day <= lastMessageDay; day = day.AddDays(1))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dayMessages = byDay[day].ToList();
                if (dayMessages.Count == 0)
                {
                    _logger.LogInformation("{Date}: no messages.", day);
                    continue;
                }

                var updated = await aiSummaryService.ExtractMemories(
                    dayMessages.Select(m => (m.DisplayName, m.Text, m.Timestamp)),
                    string.IsNullOrWhiteSpace(summary) ? null : summary,
                    cancellationToken,
                    soulPrompt);

                if (!string.IsNullOrWhiteSpace(updated))
                {
                    summary = updated.Length > Constants.Memory.MaxSummaryLength ? updated[..Constants.Memory.MaxSummaryLength] : updated;
                    // Persist after every day so progress is kept if the run is interrupted.
                    await File.WriteAllTextAsync(outputMemoryFilePath, summary, cancellationToken);
                    await File.WriteAllTextAsync(outputMemoryFilePath.Replace(".md", "-" + day.ToString("yyyy-MM-dd") + ".md"), summary, cancellationToken);
                }

                _logger.LogInformation("{Date}: processed {Count} messages, summary now {Length} chars.", day, dayMessages.Count, summary.Length);
            }

            await File.WriteAllTextAsync(outputMemoryFilePath, summary, cancellationToken);
            _logger.LogInformation("Memory rebuild complete. Final memory written to {Path} ({Length} chars).", Path.GetFullPath(outputMemoryFilePath), summary.Length);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task<List<ExportMessage>> LoadExportMessagesAsync(string exportFilePath, CancellationToken cancellationToken)
        {
            TelegramExport? export;
            await using (var stream = File.OpenRead(exportFilePath))
            {
                export = await JsonSerializer.DeserializeAsync<TelegramExport>(stream, cancellationToken: cancellationToken);
            }

            var result = new List<ExportMessage>();
            if (export?.Messages == null)
            {
                return result;
            }

            foreach (var m in export.Messages)
            {
                if (!string.Equals(m.Type, "message", StringComparison.OrdinalIgnoreCase)) continue; // skip service messages (joins, pins, etc.)
                if (string.IsNullOrWhiteSpace(m.From)) continue;

                var text = FlattenText(m.Text);
                if (string.IsNullOrWhiteSpace(text)) continue; // media-only / empty messages add nothing to memory

                var timestamp = ParseTimestamp(m);
                if (timestamp == null) continue;

                result.Add(new ExportMessage(timestamp.Value, m.From!, text));
            }

            result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return result;
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

        private sealed record ExportMessage(DateTime Timestamp, string DisplayName, string Text);

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
            [JsonPropertyName("text")] public JsonElement Text { get; set; }
        }
    }
}
