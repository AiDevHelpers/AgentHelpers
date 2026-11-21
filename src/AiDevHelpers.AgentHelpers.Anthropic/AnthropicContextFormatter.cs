using AiDevHelpers.AgentHelpers.Core.Models;
using AiDevHelpers.AgentHelpers.Core.Services;

using System.Text;
using System.Text.RegularExpressions;

namespace AiDevHelpers.AgentHelpers.Anthropic;

/// <summary>
///     Formats raw concise context into Claude-optimised XML-tagged sections.
///     Raw context structure (parts joined with "\n\n" by LocalContextService):
///     [optional] "Summary:\n{text}"
///     [optional] "Requested files: {csv}"
///     [0-5]      recent history strings
///     [1]        the user prompt (last element)
///     Output structure (stable parts first for prompt-cache eligibility):
///     &lt;workspace_summary&gt;…&lt;/workspace_summary&gt;        — summary if present
///     &lt;requested_files&gt;…&lt;/requested_files&gt;           — file list if present
///     &lt;conversation_history&gt;…&lt;/conversation_history&gt; — recent history if present
///     &lt;current_request&gt;…&lt;/current_request&gt;           — the user prompt
///     [Opus only] &lt;guidance&gt;…&lt;/guidance&gt;
/// </summary>
internal static class AnthropicContextFormatter
{
    public static string Format(string rawContext, AnthropicVariant variant, ContextFormat format)
    {
        // Apply Haiku pre-processing before splitting so line counts are already reduced
        if (variant == AnthropicVariant.Haiku)
        {
            rawContext = HaikuPreprocess(rawContext);
        }

        ParsedContextParts parts = ContextPartParser.Split(rawContext);
        StringBuilder sb = new();

        // ── Stable prefix (cache-eligible) ────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(parts.Summary))
        {
            _ = sb.AppendLine("<workspace_summary>");
            _ = sb.AppendLine(parts.Summary.Trim());
            _ = sb.AppendLine("</workspace_summary>");
            _ = sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(parts.RequestedFiles))
        {
            _ = sb.AppendLine("<requested_files>");
            _ = sb.AppendLine(parts.RequestedFiles.Trim());
            _ = sb.AppendLine("</requested_files>");
            _ = sb.AppendLine();
        }

        // ── Dynamic suffix (changes per call) ────────────────────────────────

        if (parts.History.Count > 0)
        {
            _ = sb.AppendLine("<conversation_history>");
            for (int i = 0; i < parts.History.Count; i++)
            {
                _ = sb.AppendLine(parts.History[i].Trim());
                if (i < parts.History.Count - 1)
                {
                    _ = sb.AppendLine("---");
                }
            }

            _ = sb.AppendLine("</conversation_history>");
            _ = sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(parts.Prompt))
        {
            _ = sb.AppendLine("<current_request>");
            _ = sb.AppendLine(parts.Prompt.Trim());
            _ = sb.AppendLine("</current_request>");
        }

        // ── Opus: strategy hint ───────────────────────────────────────────────

        if (variant == AnthropicVariant.Opus)
        {
            _ = sb.AppendLine();
            _ = sb.AppendLine("<guidance>");
            _ = sb.AppendLine("Analyse the workspace context thoroughly before responding.");
            _ = sb.AppendLine("Prioritise correctness and completeness over brevity.");
            _ = sb.AppendLine("</guidance>");
        }

        string result = sb.ToString().TrimEnd();
        return TokenEstimator.TrimToBudget(result, format.ContextTokenBudget);
    }

    // ── Haiku pre-processing ──────────────────────────────────────────────────

    /// <summary>
    ///     Reduces token count for Haiku by:
    ///     - Stripping single-line code comments (// …, # …) from inline code snippets
    ///     - Collapsing runs of 3+ blank lines to a single blank line
    ///     - Removing trailing whitespace per line
    /// </summary>
    private static string HaikuPreprocess(string text)
    {
        // Strip trailing spaces
        text = Regex.Replace(text, @"[ \t]+$", "", RegexOptions.Multiline);

        // Strip single-line comments inside fenced code blocks only
        // (avoids stripping # headers in markdown)
        text = StripCodeComments(text);

        // Collapse 3+ consecutive blank lines → single blank line
        text = Regex.Replace(text, @"(\r?\n){3,}", "\n\n");

        return text;
    }

    private static string StripCodeComments(string text)
    {
        // Only strip comments inside fenced code blocks (``` ... ```)
        return Regex.Replace(
            text,
            @"```[\w]*\n(.*?)```",
            m =>
            {
                string inner = Regex.Replace(m.Value, @"(?m)^\s*(//|#)[^\n]*$", "");
                return Regex.Replace(inner, @"(\r?\n){2,}", "\n");
            },
            RegexOptions.Singleline);
    }
}
