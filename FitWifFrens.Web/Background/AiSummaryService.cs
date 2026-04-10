using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitWifFrens.Web.Background
{
    public class AiSummaryService
    {
        private readonly AnthropicClient? _client;
        private readonly ILogger<AiSummaryService> _logger;

        public AiSummaryService(AnthropicClient? client, ILogger<AiSummaryService> logger)
        {
            _client = client;
            _logger = logger;

            if (_client == null)
            {
                logger.LogInformation("AiSummaryService: no Anthropic API key configured, AI messages disabled.");
            }
        }

        /// <summary>
        /// Generates a short, fun intro line for the weekly weight summary.
        /// Falls back to a default header if the AI call fails.
        /// </summary>
        public async Task<string> GenerateWeightSummaryIntro(
            IEnumerable<(string Name, double WeightChange)> weekly,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var lines = weekly
                    .Select(w => w.WeightChange < 0
                        ? $"{w.Name}: {Math.Abs(w.WeightChange):F1} kg lost"
                        : w.WeightChange > 0
                            ? $"{w.Name}: {w.WeightChange:F1} kg gained"
                            : $"{w.Name}: no change")
                    .ToList();

                var dataText = lines.Count > 0
                    ? string.Join(", ", lines)
                    : "no weigh-ins this week";

                var prompt =
                    Persona("You are a witty and encouraging fitness group coach posting a weekly weight update to a group chat. ", soulPrompt) +
                    $"Write a single short sentence (max 15 words) as an intro to the weekly weight summary. " +
                    Tone("Keep it fun, positive, and vary the style each time — sometimes motivational, sometimes playful. ", soulPrompt) +
                    $"This week's data: {dataText}. " +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken, soulPrompt) ?? "Weight Summary";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI weight summary intro generation failed, using default. Error: {Message}", ex.Message);
                return "Weight Summary";
            }
        }

        /// <summary>
        /// Generates a short, fun intro line for the weekly poll summary.
        /// Falls back to a default header if the AI call fails.
        /// </summary>
        public async Task<string> GeneratePollSummaryIntro(
            string question,
            IEnumerable<(string Name, double AverageRating)> weekly,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var lines = weekly
                    .Select(w => $"{w.Name}: avg {w.AverageRating:F1}/5")
                    .ToList();

                var dataText = lines.Count > 0
                    ? string.Join(", ", lines)
                    : "no responses this week";

                var prompt =
                    Persona("You are a witty and encouraging fitness group coach posting a weekly poll recap to a group chat. ", soulPrompt) +
                    $"The poll question was: \"{question}\". " +
                    $"Write a single short sentence (max 15 words) as an intro to the poll summary results. " +
                    Tone("Keep it fun and vary the style each time. ", soulPrompt) +
                    $"This week's ratings: {dataText}. " +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken, soulPrompt) ?? $"Q: {question}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI poll summary intro generation failed, using default. Error: {Message}", ex.Message);
                return $"Q: {question}";
            }
        }

        /// <summary>
        /// Generates a unique witty one-liner commentary per person based on their diet rating vs weight change.
        /// Falls back to the provided default commentary if the AI call fails.
        /// </summary>
        public async Task<Dictionary<string, string>> GenerateCorrelationCommentaries(
            IEnumerable<(string Name, double AvgDietRating, double WeightChange, string DefaultCommentary)> correlations,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            var correlationList = correlations.ToList();
            var result = correlationList.ToDictionary(c => c.Name, c => c.DefaultCommentary);

            try
            {
                var lines = correlationList.Select(c =>
                {
                    var weightText = c.WeightChange < 0
                        ? $"{Math.Abs(c.WeightChange):F1} kg lost"
                        : c.WeightChange > 0
                            ? $"{c.WeightChange:F1} kg gained"
                            : "no change";
                    return $"{c.Name}: diet rating {c.AvgDietRating:F1}/5, {weightText}";
                });

                var dataText = string.Join("\n", lines);

                var prompt =
                    Persona("You are a witty fitness group coach writing brief comments for a weekly group stats message. ", soulPrompt) +
                    $"For each person below, write one short sentence (max 12 words) commenting on how their diet rating compared to their actual weight change. " +
                    Tone("Be funny, varied in style, and mildly sarcastic where appropriate — but always keep it friendly. ", soulPrompt) +
                    $"If you know fun facts about a person, occasionally weave them in. " +
                    $"Return exactly one line per person in this format: NAME: comment\n\n" +
                    $"{dataText}\n\n" +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the NAME: comment lines, nothing else.";

                var response = await CallClaude(prompt, cancellationToken, soulPrompt);
                if (response == null) return result;

                foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex <= 0) continue;

                    var name = line[..colonIndex].Trim();
                    var comment = line[(colonIndex + 1)..].Trim();

                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(comment) && result.ContainsKey(name))
                    {
                        result[name] = comment;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI correlation commentary generation failed, using defaults. Error: {Message}", ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Generates a short, fun message for a real-time weigh-in notification.
        /// Falls back to the provided default message if the AI call fails.
        /// </summary>
        public async Task<string> GenerateWeighInMessage(
            string name,
            double weight,
            double? monthChange,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var changeText = monthChange.HasValue
                    ? monthChange.Value < 0
                        ? $"{Math.Abs(monthChange.Value):F1} kg lost over the past 4 weeks"
                        : monthChange.Value > 0
                            ? $"{monthChange.Value:F1} kg gained over the past 4 weeks"
                            : "no change over the past 4 weeks"
                    : "no previous data to compare";

                var prompt =
                    Persona("You are a witty fitness group coach posting a real-time weigh-in update to a group chat. ", soulPrompt) +
                    $"{name} just weighed in at {weight} kg ({changeText}). " +
                    $"Write a single short message (max 20 words) reacting to this weigh-in. " +
                    Tone("Be fun, encouraging if they lost weight, playfully teasing if they gained. Keep it friendly. ", soulPrompt) +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt);
                if (aiMessage != null)
                {
                    return $"{name} just weighed in at {weight} kg{FormatMonthChange(monthChange)}\n{aiMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI weigh-in message generation failed, using default. Error: {Message}", ex.Message);
            }

            return $"{name} just weighed in at {weight} kg{FormatMonthChange(monthChange)}";
        }

        /// <summary>
        /// Generates a short, fun message for a real-time workout notification.
        /// Falls back to the provided default message if the AI call fails.
        /// </summary>
        public async Task<string> GenerateWorkoutMessage(
            string name,
            string activityType,
            double minutes,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var prompt =
                    Persona("You are a witty fitness group coach posting a real-time workout update to a group chat. ", soulPrompt) +
                    $"{name} just logged a {activityType} for {minutes:F0} minutes. " +
                    $"Write a single short message (max 20 words) reacting to this workout. " +
                    Tone("Be fun and encouraging. Vary the style each time. Keep it friendly. ", soulPrompt) +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt);
                if (aiMessage != null)
                {
                    return $"{name} just logged a {activityType} ({minutes:F0} min)\n{aiMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI workout message generation failed, using default. Error: {Message}", ex.Message);
            }

            return $"{name} just logged a {activityType} ({minutes:F0} min)";
        }

        /// <summary>
        /// Generates a fun reminder for a user who hasn't weighed in for a while.
        /// Falls back to a default reminder if the AI call fails.
        /// </summary>
        public async Task<string> GenerateWeighInReminder(
            string name,
            int? daysSinceLastWeighIn,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var timeText = daysSinceLastWeighIn.HasValue
                    ? $"{daysSinceLastWeighIn} days"
                    : "a very long time";

                var prompt =
                    Persona("You are a witty fitness group coach posting a weigh-in reminder to a group chat. ", soulPrompt) +
                    $"{name} hasn't weighed in for {timeText}. " +
                    $"Write a single short message (max 20 words) reminding them to step on the scale. " +
                    Tone("Be fun, playful, and slightly teasing but always friendly and encouraging. Vary the style each time. ", soulPrompt) +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt);
                if (aiMessage != null)
                {
                    return aiMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI weigh-in reminder generation failed, using default. Error: {Message}", ex.Message);
            }

            var defaultTimeText = daysSinceLastWeighIn.HasValue
                ? $"{daysSinceLastWeighIn} days"
                : "a while";
            return $"Hey {name}, it's been {defaultTimeText} since your last weigh-in! Time to step on the scale!";
        }

        /// <summary>
        /// Generates a short, fun message when a user responds to a diet poll.
        /// Falls back to the provided default message if the AI call fails.
        /// </summary>
        public async Task<string> GeneratePollResponseMessage(
            string name,
            string question,
            string chosenOption,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var prompt =
                    Persona("You are a witty fitness group coach posting a real-time poll response update to a group chat. ", soulPrompt) +
                    $"{name} just answered the daily poll \"{question}\" with \"{chosenOption}\". " +
                    $"Write a single short message (max 20 words) reacting to their answer. " +
                    Tone("Be fun, playful, and vary the style each time. If their answer is positive, be encouraging. If negative, be supportive. Keep it friendly. ", soulPrompt) +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt);
                if (aiMessage != null)
                {
                    return $"{name} rated today's diet: {chosenOption}\n{aiMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI poll response message generation failed, using default. Error: {Message}", ex.Message);
            }

            return $"{name} rated today's diet: {chosenOption}";
        }

        private static string FormatFitnessData(
            string name,
            double? weightChange,
            double? avgDietRating,
            int weighInCount,
            int pollResponseCount,
            double exerciseMinutes,
            double runningMinutes,
            double workoutMinutes)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Name: {name}");
            sb.AppendLine($"Past 4 weeks stats:");

            if (weightChange.HasValue)
            {
                var changeText = weightChange.Value < 0
                    ? $"{Math.Abs(weightChange.Value):F1} kg lost"
                    : weightChange.Value > 0
                        ? $"{weightChange.Value:F1} kg gained"
                        : "no change";
                sb.AppendLine($"- Weight: {changeText}, {weighInCount} weigh-ins");
            }
            else
            {
                sb.AppendLine($"- Weight: no weigh-ins recorded");
            }

            if (pollResponseCount > 0)
            {
                sb.AppendLine($"- Diet self-rating: avg {avgDietRating:F1}/5 from {pollResponseCount} responses (3 = flat, above 3 = good, below 3 = bad)");
            }
            else
            {
                sb.AppendLine($"- Diet self-rating: no poll responses");
            }

            sb.AppendLine($"- Exercise: {exerciseMinutes:F0} total minutes");
            sb.AppendLine($"- Running: {runningMinutes:F0} minutes");
            sb.AppendLine($"- Workouts: {workoutMinutes:F0} minutes");

            return sb.ToString();
        }

        /// <summary>
        /// Generates a conversational reply when the bot is @mentioned in a group chat.
        /// Takes the user's message, recent chat history, and a summary of all members' fitness data.
        /// </summary>
        public async Task<string?> GenerateBotMentionReply(
            string senderName,
            string userMessage,
            IEnumerable<(string DisplayName, string Text, DateTime Timestamp)> recentMessages,
            string groupFitnessSummary,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? allUserFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var messageLines = recentMessages
                    .Select(m => $"[{m.Timestamp:HH:mm}] {m.DisplayName}: {m.Text}")
                    .ToList();

                var chatContext = messageLines.Count > 0
                    ? "Recent chat messages:\n" + string.Join("\n", messageLines) + "\n\n"
                    : string.Empty;

                var prompt =
                    Persona("You are a witty, knowledgeable fitness group chat assistant called FitWifFrensBot. ", soulPrompt) +
                    $"A group member named {senderName} just mentioned you with this message: \"{userMessage}\"\n\n" +
                    $"Respond directly and conversationally. Be helpful, funny, and aware of the group's fitness progress. " +
                    Tone("Keep it punchy and entertaining — you know everyone's stats and aren't afraid to call people out gently. ", soulPrompt) +
                    $"Keep your reply under 150 words.\n\n" +
                    chatContext +
                    $"Current group fitness summary:\n{groupFitnessSummary}\n" +
                    FormatFactsForPrompt(allUserFacts) +
                    $"Output only your reply, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, maxTokens: 512);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI bot mention reply generation failed. Error: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Generates a roast for a user based on their recent fitness data.
        /// </summary>
        public async Task<string?> GenerateRoast(
            string name,
            double? weightChange,
            double? avgDietRating,
            int weighInCount,
            int pollResponseCount,
            double exerciseMinutes,
            double runningMinutes,
            double workoutMinutes,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var dataText = FormatFitnessData(name, weightChange, avgDietRating, weighInCount, pollResponseCount, exerciseMinutes, runningMinutes, workoutMinutes);

                var prompt =
                    Persona("You are a savage but funny roast comedian in a fitness group chat. ", soulPrompt) +
                    $"Someone asked to be roasted based on their fitness data. They asked for this — hold nothing back. " +
                    Tone("Be absolutely ruthless, savage, and hilarious. Go for the jugular. ", soulPrompt) +
                    $"Focus on where they're slacking — low exercise, weight gain, bad diet ratings, missing weigh-ins, etc. " +
                    $"If they have known facts, use those to make the roast even more personal and devastating. " +
                    $"Write 3-5 short punchy lines. Keep each line under 15 words. Be creative and varied. Do not number the lines.\n\n" +
                    $"{dataText}\n" +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the roast lines, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI roast generation failed. Error: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Generates an encouraging poem for a user based on their recent fitness data.
        /// </summary>
        public async Task<string?> GeneratePoem(
            string name,
            double? weightChange,
            double? avgDietRating,
            int weighInCount,
            int pollResponseCount,
            double exerciseMinutes,
            double runningMinutes,
            double workoutMinutes,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var dataText = FormatFitnessData(name, weightChange, avgDietRating, weighInCount, pollResponseCount, exerciseMinutes, runningMinutes, workoutMinutes);

                var prompt =
                    Persona("You are an encouraging and warm fitness group coach who writes short poems. ", soulPrompt) +
                    $"Write a short rhyming poem (4-6 lines) celebrating this person's fitness journey. " +
                    $"Highlight what they're doing well — any progress, consistency, or effort. " +
                    Tone("If they're struggling, be gentle and motivating. Find the silver lining. ", soulPrompt) +
                    $"If they have known facts, weave those in to make it personal. " +
                    Tone("Keep it light, fun, and uplifting. ", soulPrompt) + "\n\n" +
                    $"{dataText}\n" +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the poem, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI poem generation failed. Error: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Generates an entertaining commentary about a user's current balance.
        /// </summary>
        public async Task<string?> GenerateBalanceMessage(
            string name,
            decimal balance,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null)
        {
            try
            {
                var balanceContext = balance switch
                {
                    0 => "exactly zero — broke, busted, and disgusted",
                    < 0 => $"{balance:F4} tokens (somehow negative — impressive in the worst way)",
                    < 0.01m => $"{balance:F6} tokens (a microscopic, almost insulting amount)",
                    < 0.1m => $"{balance:F4} tokens (barely enough to buy a dream)",
                    < 1m => $"{balance:F4} tokens (sub-one, haunting)",
                    < 10m => $"{balance:F4} tokens (single digits — humble beginnings)",
                    < 100m => $"{balance:F4} tokens (double digits club, respect)",
                    _ => $"{balance:F4} tokens (actually stacking — respect the grind)"
                };

                var prompt =
                    Persona("You are a wildly entertaining crypto hype commentator in a fitness accountability group chat. ", soulPrompt) +
                    $"A member just checked their FitWifFrens token balance. React in 2-3 short punchy sentences. " +
                    Tone("Be dramatic, funny, and entertaining — like a stock ticker announcer crossed with a trash-talking coach. ", soulPrompt) +
                    $"Reference their actual balance naturally. " +
                    $"If there are known facts about the person, weave those in. " +
                    $"Keep each sentence under 15 words. Be creative and varied — no generic lines.\n\n" +
                    $"User: {name}\n" +
                    $"Balance: {balanceContext}\n" +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the commentary, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI balance message generation failed. Error: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Analyzes recent chat messages and the existing memory summary to produce an updated summary document.
        /// </summary>
        public async Task<string?> ExtractMemories(
            IEnumerable<(string DisplayName, string Text, DateTime Timestamp)> recentMessages,
            string? existingSummary,
            CancellationToken cancellationToken,
            string? soulPrompt = null)
        {
            try
            {
                var messageLines = recentMessages
                    .Select(m => $"[{m.Timestamp:yyyy-MM-dd HH:mm}] {m.DisplayName}: {m.Text}")
                    .ToList();

                if (messageLines.Count == 0)
                {
                    return null;
                }

                var existingSection = !string.IsNullOrWhiteSpace(existingSummary)
                    ? $"Current summary:\n{existingSummary}\n\n"
                    : "No existing summary yet.\n\n";

                var prompt =
                    "You are maintaining a living knowledge base for a fitness group chat. " +
                    "Review the recent messages and the existing summary below, then produce an updated summary.\n\n" +
                    "Structure the summary with markdown headings:\n" +
                    "## People — a profile for each person: personality, fitness habits, goals, achievements, struggles, preferences, relationships with others\n" +
                    "## Group Dynamics — how the group interacts, rivalries, inside jokes, recurring topics, who encourages who\n" +
                    "## Current Events — ongoing challenges, recent milestones, active bets or competitions\n\n" +
                    "Guidelines:\n" +
                    "- Be detailed about each person — the more you remember about someone, the better future conversations will be\n" +
                    "- Update information that has changed rather than keeping outdated facts\n" +
                    "- Remove things that are no longer relevant\n" +
                    "- Add new information learned from the recent messages\n" +
                    "- Keep it factual and concise but comprehensive\n" +
                    "- Do NOT include trivial things (greetings, one-word reactions)\n\n" +
                    existingSection +
                    "Recent messages:\n" + string.Join("\n", messageLines) + "\n\n" +
                    "Output only the updated summary, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory extraction failed. Error: {Message}", ex.Message);
                return null;
            }
        }

        private static string FormatMonthChange(double? monthChange)
        {
            if (!monthChange.HasValue)
            {
                return string.Empty;
            }

            var change = monthChange.Value;
            var changeText = change < 0
                ? $"{Math.Abs(change):F1} kg lost"
                : change > 0
                    ? $"{change:F1} kg gained"
                    : "no change";
            return $" ({changeText} past 4 weeks)";
        }

        public static async Task<string?> LoadSoulPromptAsync(DataContext dataContext, string chatId, CancellationToken cancellationToken)
        {
            var traits = await dataContext.BotSouls
                .AsNoTracking()
                .Where(t => t.ChatId == chatId)
                .OrderBy(t => t.CreatedTime)
                .Select(t => t.Trait)
                .ToListAsync(cancellationToken);

            return FormatSoulForPrompt(traits);
        }

        public static string? FormatSoulForPrompt(IReadOnlyList<string> traits)
        {
            if (traits.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine("You have the following personality and identity. Stay in character at all times:");
            foreach (var trait in traits)
            {
                sb.AppendLine($"- {trait}");
            }

            return sb.ToString();
        }

        private static string Persona(string defaultPersona, string? soulPrompt)
            => soulPrompt != null ? "You are posting to a fitness group chat. " : defaultPersona;

        private static string Tone(string defaultTone, string? soulPrompt)
            => soulPrompt != null ? "" : defaultTone;

        private static string FormatFactsForPrompt(Dictionary<string, List<string>>? factsByName)
        {
            if (factsByName == null || factsByName.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append("Known facts about group members (use these to personalize your responses when relevant): ");
            foreach (var (name, facts) in factsByName)
            {
                foreach (var fact in facts)
                {
                    sb.Append($"{name}: {fact}. ");
                }
            }

            return sb.ToString();
        }

        private async Task<string?> CallClaude(string prompt, CancellationToken cancellationToken, string? soulPrompt = null, int maxTokens = 512)
        {
            if (_client == null) return null;

            _logger.LogInformation("AiSummaryService: calling Claude API.");

            var parameters = new MessageParameters
            {
                Messages = [new Message(RoleType.User, prompt)],
                MaxTokens = maxTokens,
                Model = "claude-haiku-4-5-20251001",
                Stream = false,
                Temperature = 1m,
                SystemMessage = soulPrompt,
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);

            _logger.LogInformation("AiSummaryService: Claude API responded. StopReason={StopReason}, ContentCount={Count}",
                response.StopReason, response.Content?.Count ?? 0);

            var text = response.Content?.OfType<TextContent>().FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("AiSummaryService: response contained no text content.");
            }

            return text;
        }
    }
}
