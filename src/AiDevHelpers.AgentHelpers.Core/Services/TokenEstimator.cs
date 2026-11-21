namespace AiDevHelpers.AgentHelpers.Core.Services;

/// <summary>
///     Fast, dependency-free token count estimator using the chars/4 heuristic.
///     Accuracy: ±10–15% vs actual BPE tokenisers, which is acceptable for context-budget
///     trimming.  We are not billing — we are preventing context-window overflow.
/// </summary>
public static class TokenEstimator
{
    private const int CharsPerToken = 4;

    /// <summary>
    ///     Estimates the number of tokens in <paramref name="text" />.
    ///     Returns 0 for null or empty input.
    /// </summary>
    public static int Estimate(string? text)
    {
        return string.IsNullOrEmpty(text) ? 0 : text.Length / CharsPerToken;
    }

    /// <summary>
    ///     Returns true if <paramref name="text" /> fits within <paramref name="tokenBudget" />.
    /// </summary>
    public static bool FitsInBudget(string? text, int tokenBudget)
    {
        return Estimate(text) <= tokenBudget;
    }

    /// <summary>
    ///     Trims <paramref name="text" /> to approximately <paramref name="tokenBudget" /> tokens
    ///     by truncating at the nearest newline boundary below the limit.
    ///     Appends a trim notice if truncation occurred.
    /// </summary>
    public static string TrimToBudget(string text, int tokenBudget)
    {
        if (FitsInBudget(text, tokenBudget))
        {
            return text;
        }

        int charLimit = tokenBudget * CharsPerToken;

        // Snap to the nearest newline below the char limit so we don't cut mid-line.
        int cutAt = text.LastIndexOf('\n', charLimit);
        if (cutAt <= 0)
        {
            cutAt = charLimit;
        }

        return text[..cutAt] + "\n\n[...context trimmed to fit token budget...]";
    }
}
