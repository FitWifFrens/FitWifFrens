using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace FitWifFrens.Web.Background
{
    public class AiSummaryService
    {
        private readonly AnthropicClient? _client;
        private readonly ILogger<AiSummaryService> _logger;

        public AiSummaryService(ILogger<AiSummaryService> logger, AnthropicClient? client = null)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Generates a short, fun intro line for the weekly weight summary.
        /// Falls back to a default header if the AI call fails.
        /// </summary>
        public async Task<string> GenerateWeightSummaryIntro(
            IEnumerable<(string Name, double WeightChange)> weekly,
            CancellationToken cancellationToken)
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
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken) ?? "Weight Summary";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI weight summary intro generation failed, using default.");
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
            CancellationToken cancellationToken)
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
                    $"Output only the sentence, no quotes, no extra text.";

                return await CallClaude(prompt, cancellationToken) ?? $"Q: {question}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI poll summary intro generation failed, using default.");
                return $"Q: {question}";
            }
        }

        /// <summary>
        /// Generates a unique witty one-liner commentary per person based on their diet rating vs weight change.
        /// Falls back to the provided default commentary if the AI call fails.
        /// </summary>
        public async Task<Dictionary<string, string>> GenerateCorrelationCommentaries(
            IEnumerable<(string Name, double AvgDietRating, double WeightChange, string DefaultCommentary)> correlations,
            CancellationToken cancellationToken)
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
                    $"Return exactly one line per person in this format: NAME: comment\n\n" +
                    $"{dataText}\n\n" +
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
                _logger.LogWarning(ex, "AI correlation commentary generation failed, using defaults.");
            }

            return result;
        }

        private async Task<string?> CallClaude(string prompt, CancellationToken cancellationToken)
        {
            if (_client == null) return null;

            var parameters = new MessageParameters
            {
                Messages = [new Message(RoleType.User, prompt)],
                MaxTokens = 512,
                Model = "claude-3-haiku-20240307",
                Temperature = 1.0m,
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            return response.Content.OfType<TextContent>().FirstOrDefault()?.Text?.Trim();
        }
    }
}
