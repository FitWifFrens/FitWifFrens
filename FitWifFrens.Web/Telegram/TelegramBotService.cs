using FitWifFrens.Data;
using FitWifFrens.Web.Background;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace FitWifFrens.Web.Telegram
{
    public sealed record TelegramPollDispatchResult(
        string PollId,
        int MessageId,
        string ChatId);

    public sealed class TelegramBotService
    {
        private sealed record PollContext(string Question, IReadOnlyList<string> Options, string ChatId, int MessageId, Guid? CommitmentId);

        private readonly BackgroundConfiguration _backgroundConfiguration;
        private readonly NotificationServiceConfiguration _notificationServiceConfiguration;
        private readonly TelegramPollResponseStore _responseStore;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly NotificationService _notificationService;
        private readonly AiSummaryService _aiSummaryService;
        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<TelegramBotService> _logger;

        private readonly ConcurrentDictionary<string, PollContext> _pollContextById = new();

        public TelegramBotService(
            BackgroundConfiguration backgroundConfiguration,
            NotificationServiceConfiguration notificationServiceConfiguration,
            TelegramPollResponseStore responseStore,
            IServiceScopeFactory serviceScopeFactory,
            NotificationService notificationService,
            AiSummaryService aiSummaryService,
            IHttpClientFactory httpClientFactory,
            TelemetryClient telemetryClient,
            ILogger<TelegramBotService> logger)
        {
            _backgroundConfiguration = backgroundConfiguration;
            _notificationServiceConfiguration = notificationServiceConfiguration;
            _responseStore = responseStore;
            _serviceScopeFactory = serviceScopeFactory;
            _notificationService = notificationService;
            _aiSummaryService = aiSummaryService;
            _httpClient = httpClientFactory.CreateClient();
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task<TelegramPollDispatchResult> SendPollAsync(
            string question,
            IReadOnlyCollection<string> options,
            string? chatId = null,
            bool allowsMultipleAnswers = false,
            bool isAnonymous = false,
            Guid? commitmentId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Poll question is required.", nameof(question));
            }

            if (options.Count < 2)
            {
                throw new ArgumentException("A Telegram poll requires at least 2 options.", nameof(options));
            }

            var optionsArray = options.ToArray();
            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/sendPoll",
                new
                {
                    chat_id = chatId ?? _notificationServiceConfiguration.ChatId,
                    question,
                    options = optionsArray,
                    allows_multiple_answers = allowsMultipleAnswers,
                    is_anonymous = isAnonymous
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var result = payload.RootElement.GetProperty("result");

            var pollId = result.GetProperty("poll").GetProperty("id").GetString()
                         ?? throw new InvalidOperationException("Telegram did not return a poll id.");

            var messageId = result.GetProperty("message_id").GetInt32();
            var resolvedChatId = result.GetProperty("chat").GetProperty("id").ToString();

            _pollContextById[pollId] = new PollContext(question, optionsArray, resolvedChatId, messageId, commitmentId);

            if (commitmentId != null)
            {
                await PersistSentCommitmentPollAsync(pollId, messageId, resolvedChatId, commitmentId.Value, cancellationToken);
            }

            return new TelegramPollDispatchResult(pollId, messageId, resolvedChatId);
        }

        public IReadOnlyCollection<TelegramPollVote> GetResponses(string pollId)
        {
            return _responseStore.GetResponses(pollId);
        }

        public async Task UpdateWebhook(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_backgroundConfiguration.CallbackUrl))
            {
                try
                {
                    await SetWebhookAsync(
                        $"{_backgroundConfiguration.CallbackUrl}/api/webhooks/telegram",
                        _notificationServiceConfiguration.WebhookSecretToken,
                        cancellationToken);

                    _logger.LogInformation("Telegram webhook registered at {CallbackUrl}/api/webhooks/telegram", _backgroundConfiguration.CallbackUrl);
                }
                catch (Exception exception)
                {
                    _telemetryClient.TrackException(exception);
                    throw;
                }
            }
        }

        public async Task SetWebhookAsync(string webhookUrl, string? secretToken = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                throw new ArgumentException("Webhook url is required.", nameof(webhookUrl));
            }

            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/setWebhook",
                new
                {
                    url = webhookUrl,
                    secret_token = secretToken,
                    allowed_updates = new[] { "poll_answer", "message" }
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task RegisterBotCommandsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/setMyCommands",
                new
                {
                    commands = new[]
                    {
                        new { command = "remember", description = "Remember a fact — e.g. /remember @Phil loves cheese" },
                        new { command = "forget", description = "Forget a fact about yourself — e.g. /forget cheese" },
                        new { command = "facts", description = "List all remembered facts about yourself" },
                        new { command = "roast", description = "Get roasted based on your fitness data" },
                        new { command = "poem", description = "Get an encouraging poem about your fitness journey" }
                    }
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Telegram bot commands registered.");
        }

        public async Task<bool> TryProcessUpdateAsync(JsonElement update, CancellationToken cancellationToken = default)
        {
            if (update.TryGetProperty("message", out var message))
            {
                return await TryProcessMessageCommandAsync(message, cancellationToken);
            }

            if (!update.TryGetProperty("poll_answer", out var pollAnswer))
            {
                return false;
            }

            var updateId = update.TryGetProperty("update_id", out var updateIdJson)
                ? updateIdJson.GetInt64()
                : -1;

            var pollId = pollAnswer.GetProperty("poll_id").GetString();
            if (string.IsNullOrWhiteSpace(pollId))
            {
                return false;
            }

            var userJson = pollAnswer.GetProperty("user");
            var userId = userJson.GetProperty("id").GetInt64();
            var username = userJson.TryGetProperty("username", out var usernameJson) ? usernameJson.GetString() : null;
            var firstName = userJson.TryGetProperty("first_name", out var firstNameJson) ? firstNameJson.GetString() : null;
            var lastName = userJson.TryGetProperty("last_name", out var lastNameJson) ? lastNameJson.GetString() : null;
            var displayName = string.Join(' ', new[] { firstName, lastName }.Where(n => !string.IsNullOrWhiteSpace(n))).Trim();

            var optionIds = pollAnswer.GetProperty("option_ids").EnumerateArray().Select(o => o.GetInt32()).ToArray();
            var answeredTime = DateTimeOffset.UtcNow;

            var vote = new TelegramPollVote(
                userId,
                username,
                string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                optionIds,
                answeredTime,
                updateId);

            _responseStore.Upsert(pollId, vote);
            await PersistPollAnswerAsync(
                updateId,
                pollId,
                userId,
                username,
                optionIds,
                answeredTime.UtcDateTime,
                cancellationToken);

            _logger.LogInformation(
                "Telegram poll response captured. PollId={PollId}, UserId={UserId}, OptionCount={OptionCount}, UpdateId={UpdateId}",
                pollId,
                userId,
                optionIds.Length,
                updateId);

            return true;
        }

        public async Task<int> PullUpdates(CancellationToken cancellationToken = default)
        {
            var lastUpdateId = _responseStore.GetLastUpdateId();
            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/getUpdates",
                new
                {
                    offset = lastUpdateId + 1,
                    timeout = 0,
                    allowed_updates = new[] { "poll_answer", "message" }
                },
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogInformation("Telegram getUpdates returned 409 Conflict — webhook is active, skipping polling.");
                return 0;
            }

            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var updates = payload.RootElement.GetProperty("result");

            var processedCount = 0;
            foreach (var update in updates.EnumerateArray())
            {
                if (await TryProcessUpdateAsync(update, cancellationToken))
                {
                    processedCount++;
                }

                if (update.TryGetProperty("update_id", out var updateIdJson))
                {
                    _responseStore.SetLastUpdateId(updateIdJson.GetInt64());
                }
            }

            if (processedCount > 0)
            {
                _telemetryClient.TrackTrace($"Processed {processedCount} Telegram poll_answer updates.");
            }

            return processedCount;
        }

        private async Task PersistSentCommitmentPollAsync(string pollId, int messageId, string chatId, Guid commitmentId, CancellationToken cancellationToken)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            if (await dataContext.CommitmentTelegramPolls.AnyAsync(p => p.PollId == pollId, cancellationToken))
            {
                return;
            }

            dataContext.CommitmentTelegramPolls.Add(new CommitmentTelegramPoll
            {
                PollId = pollId,
                CommitmentId = commitmentId,
                MessageId = messageId,
                ChatId = chatId,
                SentTime = DateTime.UtcNow
            });

            await dataContext.SaveChangesAsync(cancellationToken);
        }

        private async Task PersistPollAnswerAsync(
            long updateId,
            string pollId,
            long telegramUserId,
            string? telegramUsername,
            IReadOnlyList<int> optionIds,
            DateTime answeredTime,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                if (await dataContext.UserTelegramPollResponses.AnyAsync(r => r.UpdateId == updateId, cancellationToken))
                {
                    return;
                }

                var optionIndex = optionIds.FirstOrDefault();

                var commitmentPoll = await dataContext.CommitmentTelegramPolls
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p => p.PollId == pollId, cancellationToken);

                if (commitmentPoll == null)
                {
                    await UpsertUserTelegramPollResponseAsync(
                        dataContext,
                        updateId,
                        pollId,
                        optionIndex,
                        optionIndex + 1.0,
                        telegramUserId,
                        telegramUsername,
                        null,
                        answeredTime,
                        cancellationToken);
                    return;
                }

                var user = await dataContext.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);

                if (user == null)
                {
                    _logger.LogInformation("Telegram user not mapped to app user. TelegramUserId={TelegramUserId}", telegramUserId);
                    return;
                }

                var optionRule = await dataContext.CommitmentTelegramPollRuleOptions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(o => o.CommitmentId == commitmentPoll.CommitmentId && o.Index == optionIndex, cancellationToken);

                if (optionRule == null)
                {
                    optionRule = await dataContext.CommitmentTelegramPollRuleOptions
                        .AsNoTracking()
                        .SingleOrDefaultAsync(o => o.CommitmentId == commitmentPoll.CommitmentId && o.Index == optionIndex + 1, cancellationToken);
                }

                var responseValue = optionRule?.Value ?? optionIndex + 1.0;
                var answerDate = DateOnly.FromDateTime(answeredTime.Date);

                var commitmentPeriodUser = await dataContext.CommitmentPeriodUsers
                    .Where(cpu => cpu.CommitmentId == commitmentPoll.CommitmentId)
                    .Where(cpu => cpu.UserId == user.Id)
                    .Where(cpu => cpu.StartDate <= answerDate && answerDate < cpu.EndDate)
                    .OrderByDescending(cpu => cpu.StartDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (commitmentPeriodUser == null)
                {
                    _logger.LogInformation(
                        "Ignoring poll response because user is not committed. CommitmentId={CommitmentId}, UserId={UserId}",
                        commitmentPoll.CommitmentId,
                        user.Id);
                    return;
                }

                await UpsertUserTelegramPollResponseAsync(
                    dataContext,
                    updateId,
                    pollId,
                    optionIndex,
                    responseValue,
                    telegramUserId,
                    telegramUsername,
                    user.Id,
                    answeredTime,
                    cancellationToken);

                await UpdateTelegramPollGoalsAsync(dataContext, commitmentPeriodUser, cancellationToken);

                if (!string.IsNullOrWhiteSpace(user.Nickname))
                {
                    var question = _pollContextById.TryGetValue(pollId, out var pollContext) ? pollContext.Question : "diet poll";
                    var chosenOption = optionRule?.Text ?? $"option {optionIndex + 1}";

                    var factsRaw = await dataContext.UserFacts
                        .AsNoTracking()
                        .Where(f => f.UserId == user.Id)
                        .Select(f => f.Fact)
                        .ToListAsync(cancellationToken);
                    var userFacts = factsRaw.Count > 0
                        ? new Dictionary<string, List<string>> { { user.Nickname!, factsRaw } }
                        : null;

                    var message = await _aiSummaryService.GeneratePollResponseMessage(
                        user.Nickname!, question, chosenOption, cancellationToken, userFacts);

                    _ = _notificationService.Notify(message);
                }
            }
            catch (DbUpdateException exception)
            {
                _logger.LogWarning(exception, "Ignoring duplicate Telegram poll response for UpdateId={UpdateId}", updateId);
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }

        private static async Task UpsertUserTelegramPollResponseAsync(
            DataContext dataContext,
            long updateId,
            string pollId,
            int optionIndex,
            double value,
            long telegramUserId,
            string? telegramUsername,
            string? userId,
            DateTime answeredTime,
            CancellationToken cancellationToken)
        {
            var existingResponse = await dataContext.UserTelegramPollResponses
                .SingleOrDefaultAsync(r => r.PollId == pollId && r.TelegramUserId == telegramUserId, cancellationToken);

            if (existingResponse == null)
            {
                dataContext.UserTelegramPollResponses.Add(new UserTelegramPollResponse
                {
                    UpdateId = updateId,
                    PollId = pollId,
                    OptionIndex = optionIndex,
                    Value = value,
                    TelegramUserId = telegramUserId,
                    UserId = userId,
                    AnsweredTime = answeredTime
                });
            }
            else
            {
                existingResponse.UpdateId = updateId;
                existingResponse.OptionIndex = optionIndex;
                existingResponse.Value = value;
                existingResponse.UserId = userId;
                existingResponse.AnsweredTime = answeredTime;
                dataContext.Entry(existingResponse).State = EntityState.Modified;
            }

            await dataContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> TryProcessMessageCommandAsync(JsonElement message, CancellationToken cancellationToken)
        {
            if (!message.TryGetProperty("text", out var textJson))
            {
                return false;
            }

            var text = textJson.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var fromUser = message.GetProperty("from");
            var telegramUserId = fromUser.GetProperty("id").GetInt64();
            var chatId = message.GetProperty("chat").GetProperty("id").ToString();
            var messageId = message.GetProperty("message_id").GetInt32();

            if (text.StartsWith("/remember ", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/remember@", StringComparison.OrdinalIgnoreCase))
            {
                var spaceIndex = text.IndexOf(' ');
                if (spaceIndex < 0)
                {
                    return false;
                }

                var fact = text[(spaceIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(fact))
                {
                    await SendReplyAsync(chatId, messageId, "Please provide a fact to remember. E.g. /remember loves cheese", cancellationToken);
                    return true;
                }

                await HandleRememberAsync(telegramUserId, fact, message, chatId, messageId, cancellationToken);
                return true;
            }

            if (text.StartsWith("/forget ", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/forget@", StringComparison.OrdinalIgnoreCase))
            {
                var spaceIndex = text.IndexOf(' ');
                if (spaceIndex < 0)
                {
                    return false;
                }

                var search = text[(spaceIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(search))
                {
                    await SendReplyAsync(chatId, messageId, "Please provide text to match. E.g. /forget cheese", cancellationToken);
                    return true;
                }

                await HandleForgetAsync(telegramUserId, search, chatId, messageId, cancellationToken);
                return true;
            }

            if (text.TrimEnd().Equals("/facts", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/facts@", StringComparison.OrdinalIgnoreCase))
            {
                await HandleFactsAsync(telegramUserId, chatId, messageId, cancellationToken);
                return true;
            }

            if (text.TrimEnd().Equals("/roast", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/roast@", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/roast ", StringComparison.OrdinalIgnoreCase))
            {
                await HandleRoastAsync(telegramUserId, message, chatId, messageId, cancellationToken);
                return true;
            }

            if (text.TrimEnd().Equals("/poem", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/poem@", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("/poem ", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePoemAsync(telegramUserId, message, chatId, messageId, cancellationToken);
                return true;
            }

            return false;
        }

        private async Task<User?> ResolveTargetUserAsync(DataContext dataContext, JsonElement message, long senderTelegramUserId, CancellationToken cancellationToken)
        {
            // Check Telegram message entities for mentions with user IDs
            if (message.TryGetProperty("entities", out var entities))
            {
                foreach (var entity in entities.EnumerateArray())
                {
                    var type = entity.TryGetProperty("type", out var typeJson) ? typeJson.GetString() : null;
                    if (type != "mention" && type != "text_mention")
                    {
                        continue;
                    }

                    // text_mention has a user object with the ID directly
                    if (type == "text_mention" && entity.TryGetProperty("user", out var mentionUser))
                    {
                        var mentionTelegramUserId = mentionUser.GetProperty("id").GetInt64();
                        var user = await dataContext.Users
                            .AsNoTracking()
                            .SingleOrDefaultAsync(u => u.TelegramUserId == mentionTelegramUserId, cancellationToken);

                        if (user != null)
                        {
                            return user;
                        }
                    }

                    // Regular @mention — extract username from the message text and look up by Nickname
                    if (type == "mention" && message.TryGetProperty("text", out var fullText))
                    {
                        var offset = entity.GetProperty("offset").GetInt32();
                        var length = entity.GetProperty("length").GetInt32();
                        var mentionText = fullText.GetString()?.Substring(offset, length).TrimStart('@');

                        if (!string.IsNullOrWhiteSpace(mentionText))
                        {
                            var user = await dataContext.Users
                                .AsNoTracking()
                                .SingleOrDefaultAsync(u => u.Nickname == mentionText, cancellationToken);

                            if (user != null)
                            {
                                return user;
                            }
                        }
                    }
                }
            }

            // Fall back to the sender
            return await dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.TelegramUserId == senderTelegramUserId, cancellationToken);
        }

        private static string StripLeadingMention(string text)
        {
            if (text.StartsWith('@'))
            {
                var spaceIndex = text.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    return text[(spaceIndex + 1)..].Trim();
                }

                return string.Empty;
            }

            return text;
        }

        private async Task HandleRememberAsync(long telegramUserId, string factText, JsonElement message, string chatId, int replyToMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var targetUser = await ResolveTargetUserAsync(dataContext, message, telegramUserId, cancellationToken);

                if (targetUser == null)
                {
                    await SendReplyAsync(chatId, replyToMessageId, "Couldn't find that user. Make sure their Telegram account is linked.", cancellationToken);
                    return;
                }

                var fact = StripLeadingMention(factText);
                if (string.IsNullOrWhiteSpace(fact))
                {
                    await SendReplyAsync(chatId, replyToMessageId, "Please provide a fact after the mention. E.g. /remember @Phil loves cheese", cancellationToken);
                    return;
                }

                dataContext.UserFacts.Add(new UserFact
                {
                    UserId = targetUser.Id,
                    Fact = fact.Length > 2048 ? fact[..2048] : fact,
                    CreatedTime = DateTime.UtcNow
                });

                await dataContext.SaveChangesAsync(cancellationToken);

                var name = targetUser.Nickname ?? targetUser.UserName ?? "them";
                await SendReplyAsync(chatId, replyToMessageId, $"Got it! I'll remember that about {name}.", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle /remember command. TelegramUserId={TelegramUserId}", telegramUserId);
                await SendReplyAsync(chatId, replyToMessageId, "Something went wrong, try again later.", cancellationToken);
            }
        }

        private async Task HandleForgetAsync(long telegramUserId, string search, string chatId, int replyToMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var user = await dataContext.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);

                if (user == null)
                {
                    await SendReplyAsync(chatId, replyToMessageId, "I don't know who you are yet. Please link your Telegram account first.", cancellationToken);
                    return;
                }

                var searchLower = search.ToLowerInvariant();
                var facts = await dataContext.UserFacts
                    .Where(f => f.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                var matching = facts
                    .Where(f => f.Fact.ToLowerInvariant().Contains(searchLower))
                    .ToList();

                if (matching.Count == 0)
                {
                    await SendReplyAsync(chatId, replyToMessageId, "No matching facts found. Use /facts to see what I remember.", cancellationToken);
                    return;
                }

                dataContext.UserFacts.RemoveRange(matching);
                await dataContext.SaveChangesAsync(cancellationToken);

                var plural = matching.Count == 1 ? "fact" : "facts";
                await SendReplyAsync(chatId, replyToMessageId, $"Done! Forgot {matching.Count} {plural}.", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle /forget command. TelegramUserId={TelegramUserId}", telegramUserId);
                await SendReplyAsync(chatId, replyToMessageId, "Something went wrong, try again later.", cancellationToken);
            }
        }

        private async Task HandleFactsAsync(long telegramUserId, string chatId, int replyToMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var user = await dataContext.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);

                if (user == null)
                {
                    await SendReplyAsync(chatId, replyToMessageId, "I don't know who you are yet. Please link your Telegram account first.", cancellationToken);
                    return;
                }

                var facts = await dataContext.UserFacts
                    .AsNoTracking()
                    .Where(f => f.UserId == user.Id)
                    .OrderBy(f => f.CreatedTime)
                    .Select(f => f.Fact)
                    .ToListAsync(cancellationToken);

                if (facts.Count == 0)
                {
                    await SendReplyAsync(chatId, replyToMessageId, "I don't have any facts about you yet. Use /remember to add some!", cancellationToken);
                    return;
                }

                var name = user.Nickname ?? user.UserName ?? "you";
                var lines = facts.Select((f, i) => $"{i + 1}. {f}");
                var header = $"Here's what I remember about {name}:";
                await SendReplyAsync(chatId, replyToMessageId, $"{header}\n{string.Join("\n", lines)}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle /facts command. TelegramUserId={TelegramUserId}", telegramUserId);
                await SendReplyAsync(chatId, replyToMessageId, "Something went wrong, try again later.", cancellationToken);
            }
        }

        private sealed record UserFitnessData(
            string Name,
            double? WeightChange,
            int WeighInCount,
            double? AvgDietRating,
            int PollResponseCount,
            double ExerciseMinutes,
            double RunningMinutes,
            double WorkoutMinutes,
            Dictionary<string, List<string>>? UserFacts);

        private static async Task<UserFitnessData> GatherFitnessDataAsync(DataContext dataContext, User targetUser, CancellationToken cancellationToken)
        {
            var monthStartTime = DateTime.UtcNow.AddDays(-28);
            var name = targetUser.Nickname ?? targetUser.UserName ?? "Unknown";

            var weightValues = await dataContext.UserMetricProviderValues
                .AsNoTracking()
                .Where(v => v.UserId == targetUser.Id && v.MetricName == "Weight" && v.MetricType == MetricType.Value && v.Time >= monthStartTime)
                .OrderBy(v => v.Time)
                .Select(v => v.Value)
                .ToListAsync(cancellationToken);

            double? weightChange = weightValues.Count >= 2
                ? Math.Round(weightValues.Last() - weightValues.First(), 1)
                : null;

            var pollResponses = await dataContext.UserTelegramPollResponses
                .AsNoTracking()
                .Where(r => r.UserId == targetUser.Id && r.CommitmentPoll != null && r.AnsweredTime >= monthStartTime)
                .Select(r => r.Value)
                .ToListAsync(cancellationToken);

            var avgDietRating = pollResponses.Count > 0 ? pollResponses.Average() : (double?)null;

            var exerciseMinutes = await dataContext.UserMetricProviderValues
                .AsNoTracking()
                .Where(v => v.UserId == targetUser.Id && v.MetricName == "Exercise" && v.MetricType == MetricType.Minutes && v.Time >= monthStartTime)
                .SumAsync(v => v.Value, cancellationToken);

            var runningMinutes = await dataContext.UserMetricProviderValues
                .AsNoTracking()
                .Where(v => v.UserId == targetUser.Id && v.MetricName == "Running" && v.MetricType == MetricType.Minutes && v.Time >= monthStartTime)
                .SumAsync(v => v.Value, cancellationToken);

            var workoutMinutes = await dataContext.UserMetricProviderValues
                .AsNoTracking()
                .Where(v => v.UserId == targetUser.Id && v.MetricName == "Workout" && v.MetricType == MetricType.Minutes && v.Time >= monthStartTime)
                .SumAsync(v => v.Value, cancellationToken);

            var factsRaw = await dataContext.UserFacts
                .AsNoTracking()
                .Where(f => f.UserId == targetUser.Id)
                .Select(f => f.Fact)
                .ToListAsync(cancellationToken);
            var userFacts = factsRaw.Count > 0
                ? new Dictionary<string, List<string>> { { name, factsRaw } }
                : null;

            return new UserFitnessData(name, weightChange, weightValues.Count, avgDietRating, pollResponses.Count, exerciseMinutes, runningMinutes, workoutMinutes, userFacts);
        }

        private async Task HandleRoastAsync(long telegramUserId, JsonElement message, string chatId, int replyToMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var aiSummaryService = scope.ServiceProvider.GetRequiredService<AiSummaryService>();

                var targetUser = await ResolveTargetUserAsync(dataContext, message, telegramUserId, cancellationToken);

                if (targetUser == null)
                {
                    await SendReplyAsync(chatId, replyToMessageId, "Couldn't find that user. Make sure their Telegram account is linked.", cancellationToken);
                    return;
                }

                var data = await GatherFitnessDataAsync(dataContext, targetUser, cancellationToken);

                var roast = await aiSummaryService.GenerateRoast(
                    data.Name, data.WeightChange, data.AvgDietRating, data.WeighInCount, data.PollResponseCount,
                    data.ExerciseMinutes, data.RunningMinutes, data.WorkoutMinutes, cancellationToken, data.UserFacts);

                if (string.IsNullOrWhiteSpace(roast))
                {
                    await SendReplyAsync(chatId, replyToMessageId, $"I tried to roast {data.Name} but even AI couldn't find the words. That's how bad it is.", cancellationToken);
                    return;
                }

                await SendReplyAsync(chatId, replyToMessageId, $"Roasting {data.Name}:\n\n{roast}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle /roast command. TelegramUserId={TelegramUserId}", telegramUserId);
                await SendReplyAsync(chatId, replyToMessageId, "Something went wrong, try again later.", cancellationToken);
            }
        }

        private async Task HandlePoemAsync(long telegramUserId, JsonElement message, string chatId, int replyToMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var aiSummaryService = scope.ServiceProvider.GetRequiredService<AiSummaryService>();

                var targetUser = await ResolveTargetUserAsync(dataContext, message, telegramUserId, cancellationToken);

                if (targetUser == null)
                {
                    await SendReplyAsync(chatId, replyToMessageId, "Couldn't find that user. Make sure their Telegram account is linked.", cancellationToken);
                    return;
                }

                var data = await GatherFitnessDataAsync(dataContext, targetUser, cancellationToken);

                var poem = await aiSummaryService.GeneratePoem(
                    data.Name, data.WeightChange, data.AvgDietRating, data.WeighInCount, data.PollResponseCount,
                    data.ExerciseMinutes, data.RunningMinutes, data.WorkoutMinutes, cancellationToken, data.UserFacts);

                if (string.IsNullOrWhiteSpace(poem))
                {
                    await SendReplyAsync(chatId, replyToMessageId, $"I tried to write a poem for {data.Name} but the muse didn't show up today.", cancellationToken);
                    return;
                }

                await SendReplyAsync(chatId, replyToMessageId, $"A poem for {data.Name}:\n\n{poem}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle /poem command. TelegramUserId={TelegramUserId}", telegramUserId);
                await SendReplyAsync(chatId, replyToMessageId, "Something went wrong, try again later.", cancellationToken);
            }
        }

        private async Task SendReplyAsync(string chatId, int replyToMessageId, string text, CancellationToken cancellationToken)
        {
            await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_notificationServiceConfiguration.Token}/sendMessage",
                new
                {
                    chat_id = chatId,
                    text,
                    reply_to_message_id = replyToMessageId
                },
                cancellationToken);
        }

        private static async Task UpdateTelegramPollGoalsAsync(
            DataContext dataContext,
            CommitmentPeriodUser commitmentPeriodUser,
            CancellationToken cancellationToken)
        {
            var goals = await dataContext.CommitmentPeriodUserGoals
                .Where(g => g.CommitmentId == commitmentPeriodUser.CommitmentId &&
                            g.StartDate == commitmentPeriodUser.StartDate &&
                            g.EndDate == commitmentPeriodUser.EndDate &&
                            g.UserId == commitmentPeriodUser.UserId &&
                            g.MetricName == "Telegram Poll" &&
                            g.ProviderName == "Telegram")
                .ToListAsync(cancellationToken);

            if (!goals.Any())
            {
                return;
            }

            var startTime = commitmentPeriodUser.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).ConvertTimeToUtc();
            var endTime = commitmentPeriodUser.EndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).ConvertTimeToUtc();

            var responses = await dataContext.UserTelegramPollResponses
                .Where(r => r.UserId == commitmentPeriodUser.UserId &&
                            r.CommitmentPoll != null &&
                            r.CommitmentPoll.CommitmentId == commitmentPeriodUser.CommitmentId &&
                            r.AnsweredTime >= startTime &&
                            r.AnsweredTime < endTime)
                .ToListAsync(cancellationToken);

            foreach (var goal in goals)
            {
                goal.Value = goal.MetricType switch
                {
                    MetricType.Count => responses.Any() ? responses.Count : null,
                    MetricType.Value => responses.Any() ? Math.Round(responses.Average(r => r.Value), 2) : null,
                    _ => goal.Value
                };

                dataContext.Entry(goal).State = EntityState.Modified;
            }

            await dataContext.SaveChangesAsync(cancellationToken);
        }
    }
}
