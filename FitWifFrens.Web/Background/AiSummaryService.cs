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
