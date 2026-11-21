namespace AiDevHelpers.AgentHelpers.Core.Services;

/// <summary>
///     Splits raw concise context (as joined by LocalContextService) into its named sections.
///     Raw context structure — parts joined with "\n\n":
///     [optional] "Summary:\n{text}"
///     [optional] "Requested files: {csv}"
///     [0-5]      recent history strings
///     [1]        the user prompt (last element)
///     Shared by every AiDevHelpers.AgentHelpers.* context formatter (Anthropic, Qwen, DeepSeek,
///     OpenSource) — this parsing logic was previously duplicated verbatim across all four.
/// </summary>
public static class ContextPartParser
{
    public static ParsedContextParts Split(string raw)
    {
        string[] parts = raw.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string summary = "";
        string requestedFiles = "";
        List<string> history = [];

        foreach (string part in parts)
        {
            if (part.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
            {
                summary = part["Summary:".Length..].Trim();
            }
            else if (part.StartsWith("Requested files:", StringComparison.OrdinalIgnoreCase))
            {
                requestedFiles = part["Requested files:".Length..].Trim();
            }
            else
            {
                // All remaining parts are history entries or the final prompt.
                history.Add(part);
            }
        }

        // The last element is the user's prompt; everything before it is history.
        string prompt = "";
        if (history.Count > 0)
        {
            prompt = history[^1];
            history.RemoveAt(history.Count - 1);
        }

        return new ParsedContextParts(summary, requestedFiles, history, prompt);
    }
}

/// <summary>Result of <see cref="ContextPartParser.Split" /> — see that method for field semantics.</summary>
public sealed record ParsedContextParts(
    string Summary,
    string RequestedFiles,
    IReadOnlyList<string> History,
    string Prompt);
