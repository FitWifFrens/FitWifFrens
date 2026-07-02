using Anthropic;
using Anthropic.Models.Messages;
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
            string? soulPrompt = null,
            string? memorySummary = null)
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
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts) ?? "Weight Summary";
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
            string? soulPrompt = null,
            string? memorySummary = null)
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
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts) ?? $"Q: {question}";
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
            string? soulPrompt = null,
            string? memorySummary = null)
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
                    $"Output only the NAME: comment lines, nothing else.";

                var response = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
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
            string? soulPrompt = null,
            string? memorySummary = null,
            IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>? recentMessages = null)
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
                    FormatChatHistoryForPrompt(recentMessages) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
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
            string? soulPrompt = null,
            string? memorySummary = null,
            IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>? recentMessages = null)
        {
            try
            {
                var prompt =
                    Persona("You are a witty fitness group coach posting a real-time workout update to a group chat. ", soulPrompt) +
                    $"{name} just logged a {activityType} for {minutes:F0} minutes. " +
                    $"Write a single short message (max 20 words) reacting to this workout. " +
                    Tone("Be fun and encouraging. Vary the style each time. Keep it friendly. ", soulPrompt) +
                    FormatChatHistoryForPrompt(recentMessages) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
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
        /// Generates a short, fun message for a real-time, generic exercise notification — any
        /// physical activity that isn't already posted via a more specific path (e.g. a workout).
        /// Pass <paramref name="activityType"/> when a meaningful type is known (e.g. a Strava
        /// "Swim"/"Walk"), or null for a generic "exercise" message. Falls back to a simple
        /// default message if the AI call fails.
        /// </summary>
        public async Task<string> GenerateExerciseMessage(
            string name,
            string? activityType,
            double minutes,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null,
            string? memorySummary = null,
            IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>? recentMessages = null)
        {
            var hasType = !string.IsNullOrWhiteSpace(activityType) &&
                          !string.Equals(activityType, "Exercise", StringComparison.OrdinalIgnoreCase);

            var header = hasType
                ? $"{name} just logged a {activityType} ({minutes:F0} min)"
                : $"{name} just logged {minutes:F0} min of exercise";

            try
            {
                var activityDescription = hasType
                    ? $"{name} just logged a {activityType} for {minutes:F0} minutes. "
                    : $"{name} just logged {minutes:F0} minutes of exercise. ";

                var prompt =
                    Persona("You are a witty fitness group coach posting a real-time exercise update to a group chat. ", soulPrompt) +
                    activityDescription +
                    $"Write a single short message (max 20 words) reacting to this exercise. " +
                    Tone("Be fun and encouraging. Vary the style each time. Keep it friendly. ", soulPrompt) +
                    FormatChatHistoryForPrompt(recentMessages) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
                if (aiMessage != null)
                {
                    return $"{header}\n{aiMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI exercise message generation failed, using default. Error: {Message}", ex.Message);
            }

            return header;
        }

        /// <summary>
        /// Generates a short, fun message for a real-time blood pressure measurement notification.
        /// Falls back to a simple default message if the AI call fails.
        /// </summary>
        public async Task<string> GenerateBloodPressureMessage(
            string name,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null,
            string? memorySummary = null,
            IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>? recentMessages = null)
        {
            try
            {
                var prompt =
                    Persona("You are a witty fitness group coach posting a real-time health update to a group chat. ", soulPrompt) +
                    $"{name} just took a blood pressure measurement. " +
                    $"Write a single short message (max 20 words) reacting to it. " +
                    Tone("Be fun and lightly encouraging about them keeping on top of their health. Vary the style each time. Keep it friendly. ", soulPrompt) +
                    FormatChatHistoryForPrompt(recentMessages) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
                if (aiMessage != null)
                {
                    return $"{name} just measured their blood pressure\n{aiMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI blood pressure message generation failed, using default. Error: {Message}", ex.Message);
            }

            return $"{name} just measured their blood pressure";
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
            string? soulPrompt = null,
            string? memorySummary = null,
            IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>? recentMessages = null)
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
                    FormatChatHistoryForPrompt(recentMessages) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
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
            string? soulPrompt = null,
            string? memorySummary = null,
            IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>? recentMessages = null)
        {
            try
            {
                var prompt =
                    Persona("You are a witty fitness group coach posting a real-time poll response update to a group chat. ", soulPrompt) +
                    $"{name} just answered the daily poll \"{question}\" with \"{chosenOption}\". " +
                    $"Write a single short message (max 20 words) reacting to their answer. " +
                    Tone("Be fun, playful, and vary the style each time. If their answer is positive, be encouraging. If negative, be supportive. Keep it friendly. ", soulPrompt) +
                    FormatChatHistoryForPrompt(recentMessages) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
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

        /// <summary>
        /// Regenerates a fresh, funny label for each poll option while the caller keeps the
        /// meaning-in-parentheses constant. <paramref name="meanings"/> holds each option's
        /// fixed meaning in order; the returned list has one funny label per option in the same
        /// order. Returns null if the AI call fails or doesn't produce a clean label for every
        /// option (the caller then falls back to the original static option text).
        /// </summary>
        public async Task<IReadOnlyList<string>?> GeneratePollOptionLabels(
            string question,
            IReadOnlyList<string> meanings,
            IReadOnlyList<string> currentLabels,
            int maxLabelLength,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null,
            string? memorySummary = null)
        {
            if (meanings.Count == 0)
            {
                return null;
            }

            try
            {
                var levels = new StringBuilder();
                for (var i = 0; i < meanings.Count; i++)
                {
                    var current = i < currentLabels.Count ? currentLabels[i] : string.Empty;
                    levels.AppendLine($"{i + 1}. Meaning: {meanings[i]} | Current label (do NOT reuse): \"{current}\"");
                }

                var prompt =
                    Persona("You are a witty fitness group coach writing the answer options for a daily diet-rating poll in a group chat. ", soulPrompt) +
                    $"The poll question is: \"{question}\". " +
                    $"Below are the {meanings.Count} rating levels. Each has a fixed meaning plus the label currently in use. For each level, write a brand-new short, funny label that fits its meaning. " +
                    Tone("Be playful and vary the style every time — self-deprecating, absurd, food-pun heavy, whatever lands. ", soulPrompt) +
                    $"Match the sentiment to the meaning: positive ratings should sound proud, negative ones like cheerful chaos.\n" +
                    $"Rules:\n" +
                    $"- Exactly one label per level, in the same order.\n" +
                    $"- Each new label must be clearly DIFFERENT from the current label shown — a new joke, new angle, new wording. Do not lightly reword the current one or reuse its phrasing.\n" +
                    $"- Each label must be at most {maxLabelLength} characters.\n" +
                    $"- Do NOT include the meaning text or any parentheses — write only the funny part.\n" +
                    $"Return exactly one line per level in this format: N: label\n\n" +
                    $"Rating levels:\n{levels}\n" +
                    $"Output only the numbered label lines, nothing else.";

                var response = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
                if (response == null)
                {
                    return null;
                }

                var labels = new string?[meanings.Count];
                foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex <= 0)
                    {
                        continue;
                    }

                    if (!int.TryParse(line[..colonIndex].Trim(), out var number) || number < 1 || number > meanings.Count)
                    {
                        continue;
                    }

                    var label = line[(colonIndex + 1)..].Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(label) && label.Length <= maxLabelLength)
                    {
                        labels[number - 1] = label;
                    }
                }

                // Only use the AI labels if every option got a clean one — otherwise fall back.
                return labels.Any(l => l == null) ? null : labels!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI poll option label generation failed, using defaults. Error: {Message}", ex.Message);
                return null;
            }
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
            string? soulPrompt = null,
            string? memorySummary = null,
            IReadOnlyList<(byte[] Data, string MediaType)>? images = null)
        {
            try
            {
                var messageLines = recentMessages
                    .Select(m => m.DisplayName == "Bot"
                        ? $"[{m.Timestamp:HH:mm}] [You previously replied]: {m.Text}"
                        : $"[{m.Timestamp:HH:mm}] {m.DisplayName}: {m.Text}")
                    .ToList();

                var chatContext = messageLines.Count > 0
                    ? "Recent chat messages:\n" + string.Join("\n", messageLines) + "\n\n"
                    : string.Empty;

                var hasImages = images != null && images.Count > 0;
                var imageNote = hasImages
                    ? $"{senderName} also shared an image — examine it and weave your observations into the reply when relevant.\n\n"
                    : string.Empty;

                var prompt =
                    Persona("You are a witty, knowledgeable fitness group chat assistant called FitWifFrensBot. ", soulPrompt) +
                    $"A group member named {senderName} just mentioned you with this message: \"{userMessage}\"\n\n" +
                    imageNote +
                    $"Respond directly and conversationally. Match your reply length to what the message actually calls for — " +
                    $"a simple question or banter deserves a short punchy reply (1-2 sentences), while something that needs a real answer can be a bit longer. " +
                    Tone("Be funny and aware of the group's fitness stats — don't be afraid to call people out gently. ", soulPrompt) +
                    $"Do NOT repeat or rephrase anything you have already said in this conversation — vary your angle, tone, or focus each time.\n\n" +
                    chatContext +
                    $"Current group fitness summary:\n{groupFitnessSummary}\n" +
                    $"Output only your reply, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, maxTokens: 512, images: images, enableWebSearch: true, memorySummary: memorySummary, userFacts: allUserFacts);
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
            string? soulPrompt = null,
            string? memorySummary = null)
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
                    $"Output only the roast lines, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
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
            string? soulPrompt = null,
            string? memorySummary = null)
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
                    $"Output only the poem, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
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
            string? soulPrompt = null,
            string? memorySummary = null)
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
                    $"Output only the commentary, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI balance message generation failed. Error: {Message}", ex.Message);
                return null;
            }
        }

        public sealed record CommitmentPeriodAiSummary(string? Intro, Dictionary<string, string> Commentaries);

        /// <summary>
        /// Generates a fun intro line plus a personalised one-liner for each winner/loser of a completed commitment period.
        /// </summary>
        public async Task<CommitmentPeriodAiSummary> GenerateCommitmentPeriodSummary(
            string commitmentTitle,
            IEnumerable<(string Name, decimal Stake, decimal Reward)> winners,
            IEnumerable<(string Name, decimal Stake)> losers,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null,
            string? memorySummary = null)
        {
            var winnerList = winners.ToList();
            var loserList = losers.ToList();
            var empty = new CommitmentPeriodAiSummary(null, new Dictionary<string, string>());

            try
            {
                if (!winnerList.Any() && !loserList.Any())
                {
                    return empty;
                }

                var winnerLines = winnerList.Count > 0
                    ? string.Join("\n", winnerList.Select(w => $"WINNER {w.Name}: staked {w.Stake:0.####}, won {w.Reward:0.####}"))
                    : "(none)";

                var loserLines = loserList.Count > 0
                    ? string.Join("\n", loserList.Select(l => $"LOSER {l.Name}: forfeited {l.Stake:0.####}"))
                    : "(none)";

                var prompt =
                    Persona("You are a witty fitness group coach announcing the results of a completed commitment period in a group chat. ", soulPrompt) +
                    $"The commitment was: \"{commitmentTitle}\". " +
                    $"Write an announcement in this exact format, one item per line:\n" +
                    $"INTRO: <one short sentence (max 18 words) hyping the results — celebrate winners, playfully roast losers>\n" +
                    $"NAME: <one short sentence (max 14 words) addressed to that person — encouraging for winners, teasing-but-friendly for losers>\n" +
                    $"Include one NAME line for every person below. Use their name exactly as given.\n" +
                    Tone("Vary the style each time. Be funny, be personal where facts allow, never mean-spirited. ", soulPrompt) +
                    $"\nResults:\n{winnerLines}\n{loserLines}\n\n" +
                    $"Output only the INTRO and NAME lines, nothing else.";

                var response = await CallClaude(prompt, cancellationToken, soulPrompt, memorySummary: memorySummary, userFacts: userFacts);
                if (response == null) return empty;

                string? intro = null;
                var commentaries = new Dictionary<string, string>();
                var validNames = winnerList.Select(w => w.Name).Concat(loserList.Select(l => l.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex <= 0) continue;

                    var key = line[..colonIndex].Trim();
                    var value = line[(colonIndex + 1)..].Trim();
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    if (string.Equals(key, "INTRO", StringComparison.OrdinalIgnoreCase))
                    {
                        intro = value;
                    }
                    else if (validNames.Contains(key))
                    {
                        commentaries[key] = value;
                    }
                }

                return new CommitmentPeriodAiSummary(intro, commentaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI commitment period summary generation failed. Error: {Message}", ex.Message);
                return empty;
            }
        }

        /// <summary>
        /// Given a structured snapshot of the group's current state, lets the model decide whether
        /// there is anything worth saying right now. Returns null if the model elects to stay silent
        /// (or if the AI client is not configured).
        /// </summary>
        public async Task<string?> GenerateAmbientMessage(
            string contextSnapshot,
            CancellationToken cancellationToken,
            Dictionary<string, List<string>>? userFacts = null,
            string? soulPrompt = null,
            string? memorySummary = null)
        {
            try
            {
                var prompt =
                    Persona("You are a witty fitness group coach hanging out in a Telegram group chat. ", soulPrompt) +
                    "You have just been woken up and handed a snapshot of the group's current state. " +
                    "Your job: decide whether there is something genuinely worth posting to the chat right now — " +
                    "a milestone hit, a streak broken, a missed weigh-in worth nudging, a quiet day worth stirring up, " +
                    "something the recent chat hinted at, or just good timing for a check-in. " +
                    "If nothing meaningful jumps out, stay silent — do NOT manufacture a reason to post.\n\n" +
                    "Output rules:\n" +
                    "- If you decide to post: output ONLY the message text (max ~50 words, 1-3 short sentences). " +
                    "No quotes, no preamble, no \"Here's a message:\". Just the message.\n" +
                    "- If you decide not to post: output the single word SKIP and nothing else.\n" +
                    Tone("Vary tone — sometimes hyped, sometimes teasing, sometimes warm. Never generic. ", soulPrompt) +
                    "Reference specific people, numbers, or recent events from the snapshot to ground the message. " +
                    "Avoid repeating something you've already said in the recent chat history.\n\n" +
                    "Current snapshot:\n" + contextSnapshot;

                var response = await CallClaude(prompt, cancellationToken, soulPrompt, maxTokens: 512, memorySummary: memorySummary, userFacts: userFacts);
                if (string.IsNullOrWhiteSpace(response))
                {
                    return null;
                }

                var trimmed = response.Trim().TrimEnd('.', '!', '?').Trim();
                if (string.Equals(trimmed, "SKIP", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return response.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ambient message generation failed. Error: {Message}", ex.Message);
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
                    "- Do NOT include trivial things (greetings, one-word reactions)\n" +
                    "- IMPORTANT: the whole summary must fit within a strict size budget — keep it under roughly 10000 words. " +
                    "If adding new information would push it over, condense the wording and drop the least important or oldest details " +
                    "(prioritise keeping recent and recurring information) so the complete summary always fits within the budget\n\n" +
                    existingSection +
                    "Recent messages:\n" + string.Join("\n", messageLines) + "\n\n" +
                    "Output only the updated summary, nothing else.";

                return await CallClaude(prompt, cancellationToken, soulPrompt, maxTokens: 16_384);
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

        public static async Task<string?> LoadMemorySummaryAsync(DataContext dataContext, string chatId, CancellationToken cancellationToken)
        {
            var memory = await dataContext.BotMemories
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .Select(m => m.Summary)
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(memory) ? null : memory;
        }

        private static string FormatMemoryForPrompt(string? memorySummary)
        {
            if (string.IsNullOrWhiteSpace(memorySummary))
            {
                return string.Empty;
            }

            return $"You have the following long-term memory about this group and its members. Use this to personalize your responses:\n{memorySummary}\n\n";
        }

        private static string FormatChatHistoryForPrompt(IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>? recentMessages)
        {
            if (recentMessages == null || recentMessages.Count == 0)
            {
                return string.Empty;
            }

            var lines = recentMessages.Select(m => m.DisplayName == "Bot"
                ? $"[{m.Timestamp:HH:mm}] [You previously said]: {m.Text}"
                : $"[{m.Timestamp:HH:mm}] {m.DisplayName}: {m.Text}");

            return "Recent chat messages (so you know what's currently being discussed — react naturally to it and don't repeat yourself):\n" +
                   string.Join("\n", lines) + "\n\n";
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

        /// <summary>
        /// The baseline context the bot has whenever it speaks: its personality (soul), long-term
        /// memory, known facts about every member, and the recent chat history — i.e. the same
        /// memory and current history it has when replying to a mention. Individual message types
        /// layer their own specifics (a weigh-in value, an activity, a poll answer, ...) on top.
        /// </summary>
        public sealed record BotChatContext(
            string? SoulPrompt,
            string? MemorySummary,
            Dictionary<string, List<string>>? AllUserFacts,
            IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)> RecentMessages);

        /// <summary>
        /// Loads the standard <see cref="BotChatContext"/> for a chat so every message the bot sends
        /// is grounded in the same memory and current history, regardless of what triggered it.
        /// </summary>
        public static async Task<BotChatContext> LoadChatContextAsync(DataContext dataContext, string chatId, CancellationToken cancellationToken)
        {
            var soulPrompt = await LoadSoulPromptAsync(dataContext, chatId, cancellationToken);
            var memorySummary = await LoadMemorySummaryAsync(dataContext, chatId, cancellationToken);
            var allUserFacts = await LoadAllUserFactsAsync(dataContext, cancellationToken);
            var recentMessages = await LoadRecentMessagesAsync(dataContext, chatId, cancellationToken);

            return new BotChatContext(soulPrompt, memorySummary, allUserFacts, recentMessages);
        }

        /// <summary>
        /// Loads the known facts for every group member, keyed by display name — so the bot knows
        /// the whole group, not just whoever triggered the current message.
        /// </summary>
        public static async Task<Dictionary<string, List<string>>?> LoadAllUserFactsAsync(DataContext dataContext, CancellationToken cancellationToken)
        {
            var rows = await dataContext.UserFacts
                .AsNoTracking()
                .Where(f => f.User.TelegramUserId != null)
                .Select(f => new { f.User.Nickname, f.User.UserName, f.Fact })
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                return null;
            }

            return rows
                .GroupBy(r => r.Nickname ?? r.UserName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Select(r => r.Fact).ToList());
        }

        /// <summary>
        /// Loads the most recent chat messages (oldest-first) so the bot has the current conversation
        /// as context — the same window used when it replies to a mention.
        /// </summary>
        public static async Task<IReadOnlyList<(string DisplayName, string Text, DateTime Timestamp)>> LoadRecentMessagesAsync(DataContext dataContext, string chatId, CancellationToken cancellationToken)
        {
            var rows = await dataContext.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.Timestamp)
                .Take(Constants.Memory.MentionContextMessageCount)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { m.DisplayName, m.Text, m.Timestamp })
                .ToListAsync(cancellationToken);

            return rows.Select(m => (m.DisplayName, m.Text, m.Timestamp)).ToList();
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

        private async Task<string?> CallClaude(string prompt, CancellationToken cancellationToken, string? soulPrompt = null, int maxTokens = 512, IReadOnlyList<(byte[] Data, string MediaType)>? images = null, bool enableWebSearch = false, string? memorySummary = null, Dictionary<string, List<string>>? userFacts = null)
        {
            if (_client == null) return null;

            _logger.LogInformation("AiSummaryService: calling Claude API. Images={ImageCount}, WebSearch={WebSearch}", images?.Count ?? 0, enableWebSearch);

            MessageParam userMessage;
            if (images != null && images.Count > 0)
            {
                var content = new List<ContentBlockParam>(images.Count + 1);
                foreach (var (data, mediaType) in images)
                {
                    content.Add(new ImageBlockParam
                    {
                        Source = new Base64ImageSource
                        {
                            MediaType = mediaType,
                            Data = Convert.ToBase64String(data),
                        }
                    });
                }
                content.Add(new TextBlockParam { Text = prompt });
                userMessage = new MessageParam { Role = Anthropic.Models.Messages.Role.User, Content = content };
            }
            else
            {
                userMessage = new MessageParam { Role = Anthropic.Models.Messages.Role.User, Content = prompt };
            }

            // The bot's personality (soul), long-term memory, and known facts about the
            // group are identical across the many messages it sends in a chat. Putting them
            // in the system prompt (rather than rebuilding them into every user message) lets
            // us cache this large, stable prefix so it isn't re-processed on every call.
            var systemBlocks = new List<TextBlockParam>();
            if (soulPrompt != null)
            {
                systemBlocks.Add(new TextBlockParam { Text = soulPrompt });
            }

            var memoryText = FormatMemoryForPrompt(memorySummary);
            if (memoryText.Length > 0)
            {
                systemBlocks.Add(new TextBlockParam { Text = memoryText });
            }

            var factsText = FormatFactsForPrompt(userFacts);
            if (factsText.Length > 0)
            {
                systemBlocks.Add(new TextBlockParam { Text = factsText });
            }

            // Cache the system block (personality + memory + facts) so repeated calls that
            // share this prefix are billed at the reduced cache-read rate. The breakpoint
            // goes on the last block, which caches everything before it too. A 1h TTL is used
            // because the memory summary is only rebuilt once a day, so the prefix stays
            // stable — and every cache read refreshes the window, keeping an active chat warm
            // off a single write. CacheControl is init-only, so rebuild the last block with it set.
            if (systemBlocks.Count > 0)
            {
                systemBlocks[^1] = new TextBlockParam
                {
                    Text = systemBlocks[^1].Text,
                    CacheControl = new CacheControlEphemeral { Ttl = Ttl.Ttl1h },
                };
            }

            // Offer the web search tool on the paths that need it — Claude decides per-message
            // whether to actually use it.
            var tools = enableWebSearch
                ? new List<ToolUnion> { new ToolUnion(new WebSearchTool20260209 { MaxUses = 3 }) }
                : null;

            var parameters = new MessageCreateParams
            {
                Messages = [userMessage],
                MaxTokens = maxTokens,
                Model = Model.ClaudeSonnet5,
                Temperature = 1,
                System = systemBlocks,
                // These are short chat reactions — disable thinking so the model doesn't
                // spend the (small) MaxTokens budget on reasoning. Sonnet 5 turns adaptive
                // thinking on by default when this is omitted.
                Thinking = new ThinkingConfigDisabled(),
                Tools = tools,
            };

            var response = await _client.Messages.Create(parameters);

            // Token usage — including cache write (CacheCreationInputTokens) and cache read
            // (CacheReadInputTokens) — so cache effectiveness can be confirmed from the logs.
            // A healthy cache shows CacheReadTokens > 0 on repeat calls that share the prefix.
            _logger.LogInformation(
                "AiSummaryService: Claude API responded. StopReason={StopReason}, ContentCount={Count}, InputTokens={InputTokens}, OutputTokens={OutputTokens}, CacheWriteTokens={CacheWriteTokens}, CacheReadTokens={CacheReadTokens}",
                response.StopReason, response.Content.Count, response.Usage.InputTokens, response.Usage.OutputTokens,
                response.Usage.CacheCreationInputTokens, response.Usage.CacheReadInputTokens);

            // A web search turn can return multiple text blocks interleaved with tool/result
            // blocks — join them so the full reply is captured, not just the first fragment.
            var text = string.Join("\n\n", response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .Select(t => t.Text?.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t)));

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("AiSummaryService: response contained no text content.");
                return null;
            }

            return text;
        }
    }
}
