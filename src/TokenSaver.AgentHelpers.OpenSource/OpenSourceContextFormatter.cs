using TokenSaver.AgentHelpers.Core.Models;
using TokenSaver.AgentHelpers.Core.Services;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TokenSaver.AgentHelpers.OpenSource;

/// <summary>
///     Formats raw concise context for Llama, Mistral, and Gemma models.
///     Llama output:
///     System prompt (2–3 sentences):
///     "You are an expert software engineering assistant. …"
///     User turn:
///     ## Workspace Summary
///     {summary}
///     ## Requested Files
///     {file list}
///     ## Conversation History
///     {history}
///     ## Task
///     {prompt}
///     Mistral output:
///     System prompt (1 sentence):
///     "You are an expert coding assistant."
///     User turn — same sections but rendered as bullet lists instead of prose:
///     ## Workspace Summary
///     - {bullet per sentence}
///     ## Requested Files
///     - file1.cs
///     - file2.md
///     ## History
///     {entries}
///     ## Task
///     {prompt}
///     Gemma output (no system message — folded into user turn):
///     You are an expert coding assistant.
///     ---
///     ## Workspace Summary
///     {summary}
///     ## Requested Files
///     {files}
///     ## Task
///     {prompt}
/// </summary>
internal static class OpenSourceContextFormatter
{
    public static string Format(string rawContext, OpenSourceVariant variant, ContextFormat format)
    {
        ParsedContextParts parts = ContextPartParser.Split(rawContext);

        return variant switch
        {
            OpenSourceVariant.Mistral => FormatMistral(parts, format),
            OpenSourceVariant.Gemma => FormatGemma(parts, format),
            _ => FormatLlama(parts, format)
        };
    }

    // ── Llama ─────────────────────────────────────────────────────────────────

    private static string FormatLlama(ParsedContextParts parts, ContextFormat format)
    {
        StringBuilder sb = new();

        // System prompt: 2–3 sentences, explicit task framing
        // (Ollama applies the <|header_start|>system<|header_end|> wrapping automatically)
        _ = sb.AppendLine("You are an expert software engineering assistant.");
        _ = sb.AppendLine("You have been given workspace context below.");
        _ = sb.AppendLine("Base all responses strictly on the provided context.");
        _ = sb.AppendLine();

        AppendMarkdownSections(sb, parts, false);

        string result = sb.ToString().TrimEnd();
        return TokenEstimator.TrimToBudget(result, format.ContextTokenBudget);
    }

    // ── Mistral ───────────────────────────────────────────────────────────────

    private static string FormatMistral(ParsedContextParts parts, ContextFormat format)
    {
        StringBuilder sb = new();

        // System prompt: one sentence — research shows more hurts Mistral
        _ = sb.AppendLine("You are an expert coding assistant.");
        _ = sb.AppendLine();

        AppendMarkdownSections(sb, parts, true);

        string result = sb.ToString().TrimEnd();
        return TokenEstimator.TrimToBudget(result, format.ContextTokenBudget);
    }

    // ── Gemma ─────────────────────────────────────────────────────────────────

    private static string FormatGemma(ParsedContextParts parts, ContextFormat format)
    {
        // Gemma has no separate system message.
        // System role text goes first in the user turn, separated by "---".
        StringBuilder sb = new();

        _ = sb.AppendLine("You are an expert coding assistant.");
        _ = sb.AppendLine("---");

        AppendMarkdownSections(sb, parts, false);

        string result = sb.ToString().TrimEnd();
        return TokenEstimator.TrimToBudget(result, format.ContextTokenBudget);
    }

    // ── Shared section builder ────────────────────────────────────────────────

    private static void AppendMarkdownSections(StringBuilder sb, ParsedContextParts parts, bool bulletize)
    {
        if (!string.IsNullOrWhiteSpace(parts.Summary))
        {
            _ = sb.AppendLine("## Workspace Summary");
            _ = sb.AppendLine(bulletize ? Bulletize(parts.Summary) : parts.Summary.Trim());
            _ = sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(parts.RequestedFiles))
        {
            _ = sb.AppendLine("## Requested Files");
            if (bulletize)
            {
                // File list is CSV — turn into one bullet per file
                string[] files = parts.RequestedFiles.Split(',',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (string f in files)
                {
                    _ = sb.AppendLine(CultureInfo.InvariantCulture, $"- {f}");
                }
            }
            else
            {
                _ = sb.AppendLine(parts.RequestedFiles.Trim());
            }

            _ = sb.AppendLine();
        }

        if (parts.History.Count > 0)
        {
            _ = sb.AppendLine("## Conversation History");
            for (int i = 0; i < parts.History.Count; i++)
            {
                _ = sb.AppendLine(parts.History[i].Trim());
                if (i < parts.History.Count - 1)
                {
                    _ = sb.AppendLine("\n---");
                }
            }

            _ = sb.AppendLine();
        }

        _ = sb.AppendLine("## Task");
        _ = sb.AppendLine(parts.Prompt.Trim());
    }

    // ── Bulletize helper ──────────────────────────────────────────────────────

    /// <summary>
    ///     Converts a prose paragraph into a Markdown bullet list by splitting on
    ///     sentence-ending punctuation. Falls back to the original text for very
    ///     short inputs or inputs that are already bullet lists.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    private static string Bulletize(string text)
    {
        text = text.Trim();

        // Already a list — return as-is
        if (text.StartsWith('-') || text.StartsWith('*') || text.StartsWith("1.", StringComparison.Ordinal))
        {
            return text;
        }

        // Split on sentence boundaries (. ! ?) followed by whitespace or end-of-string
        List<string> sentences = [];
        foreach (string s in Regex.Split(text, @"(?<=[.!?])\s+"))
        {
            string s1 = s.Trim();
            if (!string.IsNullOrEmpty(s1))
            {
                sentences.Add(s1);
            }
        }

        // Not worth bulletizing if there's only one sentence
        return sentences.Count <= 1 ? text : string.Join("\n", sentences.Select(s => $"- {s}"));
    }
}
