using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
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
            Dictionary<string, List<string>>? userFacts = null)
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
                    $"You are a witty and encouraging fitness group coach posting a weekly weight update to a group chat. " +
                    $"Write a single short sentence (max 15 words) as an intro to the weekly weight summary. " +
                    $"Keep it fun, positive, and vary the style each time — sometimes motivational, sometimes playful. " +
                    $"This week's data: {dataText}. " +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken) ?? "Weight Summary";
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
            Dictionary<string, List<string>>? userFacts = null)
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
                    $"You are a witty and encouraging fitness group coach posting a weekly poll recap to a group chat. " +
                    $"The poll question was: \"{question}\". " +
                    $"Write a single short sentence (max 15 words) as an intro to the poll summary results. " +
                    $"Keep it fun and vary the style each time. " +
                    $"This week's ratings: {dataText}. " +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken) ?? $"Q: {question}";
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
            Dictionary<string, List<string>>? userFacts = null)
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
                    $"You are a witty fitness group coach writing brief comments for a weekly group stats message. " +
                    $"For each person below, write one short witty sentence (max 12 words) commenting on how their diet rating compared to their actual weight change. " +
                    $"Be funny, varied in style, and mildly sarcastic where appropriate — but always keep it friendly. " +
                    $"If you know fun facts about a person, occasionally weave them in. " +
                    $"Return exactly one line per person in this format: NAME: comment\n\n" +
                    $"{dataText}\n\n" +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the NAME: comment lines, nothing else.";

                var response = await CallClaude(prompt, cancellationToken);
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
            Dictionary<string, List<string>>? userFacts = null)
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
                    $"You are a witty fitness group coach posting a real-time weigh-in update to a group chat. " +
                    $"{name} just weighed in at {weight} kg ({changeText}). " +
                    $"Write a single short message (max 20 words) reacting to this weigh-in. " +
                    $"Be fun, encouraging if they lost weight, playfully teasing if they gained. Keep it friendly. " +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken);
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
            Dictionary<string, List<string>>? userFacts = null)
        {
            try
            {
                var prompt =
                    $"You are a witty fitness group coach posting a real-time workout update to a group chat. " +
                    $"{name} just logged a {activityType} for {minutes:F0} minutes. " +
                    $"Write a single short message (max 20 words) reacting to this workout. " +
                    $"Be fun and encouraging. Vary the style each time. Keep it friendly. " +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the message, no quotes, no extra text.";

                var aiMessage = await CallClaude(prompt, cancellationToken);
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
            Dictionary<string, List<string>>? userFacts = null)
        {
            try
            {
                var dataText = FormatFitnessData(name, weightChange, avgDietRating, weighInCount, pollResponseCount, exerciseMinutes, runningMinutes, workoutMinutes);

                var prompt =
                    $"You are a savage but funny roast comedian in a fitness group chat. " +
                    $"Someone asked to be roasted based on their fitness data. Be brutally honest and hilarious. " +
                    $"Focus on where they're slacking — low exercise, weight gain, bad diet ratings, missing weigh-ins, etc. " +
                    $"If they have known facts, use those to make the roast more personal and cutting. " +
                    $"Write 3-5 short punchy lines. Keep each line under 15 words. Be creative and varied. " +
                    $"Keep it friendly enough that they'll laugh, not cry.\n\n" +
                    $"{dataText}\n" +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the roast lines, nothing else.";

                return await CallClaude(prompt, cancellationToken);
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
            Dictionary<string, List<string>>? userFacts = null)
        {
            try
            {
                var dataText = FormatFitnessData(name, weightChange, avgDietRating, weighInCount, pollResponseCount, exerciseMinutes, runningMinutes, workoutMinutes);

                var prompt =
                    $"You are an encouraging and warm fitness group coach who writes short poems. " +
                    $"Write a short rhyming poem (4-6 lines) celebrating this person's fitness journey. " +
                    $"Highlight what they're doing well — any progress, consistency, or effort. " +
                    $"If they're struggling, be gentle and motivating. Find the silver lining. " +
                    $"If they have known facts, weave those in to make it personal. " +
                    $"Keep it light, fun, and uplifting.\n\n" +
                    $"{dataText}\n" +
                    FormatFactsForPrompt(userFacts) +
                    $"Output only the poem, nothing else.";

                return await CallClaude(prompt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI poem generation failed. Error: {Message}", ex.Message);
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

        private async Task<string?> CallClaude(string prompt, CancellationToken cancellationToken)
        {
            if (_client == null) return null;

            _logger.LogInformation("AiSummaryService: calling Claude API.");

            var parameters = new MessageParameters
            {
                Messages = [new Message(RoleType.User, prompt)],
                MaxTokens = 512,
                Model = "claude-haiku-4-5-20251001",
                Stream = false,
                Temperature = 1m,
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
