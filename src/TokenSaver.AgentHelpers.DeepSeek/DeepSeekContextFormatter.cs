using TokenSaver.AgentHelpers.Core.Models;
using TokenSaver.AgentHelpers.Core.Services;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TokenSaver.AgentHelpers.DeepSeek;

/// <summary>
///     Formats raw concise context for DeepSeek models.
///     R1 output — everything in one flat user-message block, no system prompt:
///     [CONTEXT]
///     {summary if present}
///     Requested files: {csv}
///     {history entries}
///     [/CONTEXT]
///     {prompt — direct, no preamble}
///     V3 / Coder output — structured with XML sections and Markdown headers:
///     System prompt: "You are a helpful coding assistant. …"
///     &lt;workspace&gt;
///     ## Summary
///     {summary}
///     ## Files
///     {file list}
///     &lt;/workspace&gt;
///     &lt;history&gt;
///     {entries}
///     &lt;/history&gt;
///     &lt;instructions&gt;
///     {prompt}
///     &lt;/instructions&gt;
///     Coder additionally annotates each path reference with a fenced-code hint.
/// </summary>
internal static class DeepSeekContextFormatter
{
    public static string Format(string rawContext, DeepSeekVariant variant, ContextFormat format)
    {
        ParsedContextParts parts = ContextPartParser.Split(rawContext);

        return variant switch
        {
            DeepSeekVariant.R1 => FormatR1(parts, format),
            DeepSeekVariant.Coder => FormatCoder(parts, format),
            _ => FormatV3(parts, format)
        };
    }

    // ── R1 formatter — flat, no system prompt ─────────────────────────────────

    private static string FormatR1(ParsedContextParts parts, ContextFormat format)
    {
        // R1: everything goes into the user message body.
        // System prompt must be left empty by the caller — we signal this
        // by NOT including any "You are..." preamble; the adapter's
        // ContextFormat.SupportsSystemPrompt = false tells the tool handler.
        StringBuilder sb = new();

        // Context block — clearly delimited so R1 knows where context ends
        bool hasContext = !string.IsNullOrWhiteSpace(parts.Summary)
                         || !string.IsNullOrWhiteSpace(parts.RequestedFiles)
                         || parts.History.Count > 0;

        if (hasContext)
        {
            _ = sb.AppendLine("[CONTEXT]");

            if (!string.IsNullOrWhiteSpace(parts.Summary))
            {
                _ = sb.AppendLine(parts.Summary.Trim());
                _ = sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(parts.RequestedFiles))
            {
                _ = sb.AppendLine(CultureInfo.InvariantCulture, $"Requested files: {parts.RequestedFiles.Trim()}");
                _ = sb.AppendLine();
            }

            for (int i = 0; i < parts.History.Count; i++)
            {
                _ = sb.AppendLine(parts.History[i].Trim());
                if (i < parts.History.Count - 1)
                {
                    _ = sb.AppendLine();
                }
            }

            _ = sb.AppendLine("[/CONTEXT]");
            _ = sb.AppendLine();
        }

        // Task — state it directly, no "Please" or preamble
        _ = sb.Append(parts.Prompt.Trim());

        string result = sb.ToString();
        return TokenEstimator.TrimToBudget(result, format.ContextTokenBudget);
    }

    // ── V3 formatter — XML sections, Markdown headers ────────────────────────

    private static string FormatV3(ParsedContextParts parts, ContextFormat format)
    {
        StringBuilder sb = new();

        // Workspace block (summary + file list) — stable content grouped together
        bool hasWorkspace = !string.IsNullOrWhiteSpace(parts.Summary)
                           || !string.IsNullOrWhiteSpace(parts.RequestedFiles);
        if (hasWorkspace)
        {
            _ = sb.AppendLine("<workspace>");

            if (!string.IsNullOrWhiteSpace(parts.Summary))
            {
                _ = sb.AppendLine("## Summary");
                _ = sb.AppendLine(parts.Summary.Trim());
                _ = sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(parts.RequestedFiles))
            {
                _ = sb.AppendLine("## Files");
                _ = sb.AppendLine(parts.RequestedFiles.Trim());
            }

            _ = sb.AppendLine("</workspace>");
            _ = sb.AppendLine();
        }

        // History block
        if (parts.History.Count > 0)
        {
            _ = sb.AppendLine("<history>");
            for (int i = 0; i < parts.History.Count; i++)
            {
                _ = sb.AppendLine(parts.History[i].Trim());
                if (i < parts.History.Count - 1)
                {
                    _ = sb.AppendLine("\n---");
                }
            }

            _ = sb.AppendLine("</history>");
            _ = sb.AppendLine();
        }

        // Instructions block — the user's actual request
        _ = sb.AppendLine("<instructions>");
        _ = sb.AppendLine(parts.Prompt.Trim());
        _ = sb.AppendLine("</instructions>");

        string result = sb.ToString().TrimEnd();
        return TokenEstimator.TrimToBudget(result, format.ContextTokenBudget);
    }

    // ── Coder formatter — V3 base + file path annotations ────────────────────

    private static string FormatCoder(ParsedContextParts parts, ContextFormat format)
    {
        // Run V3 first, then post-process to annotate file paths in the context
        string v3 = FormatV3(parts, format);
        return AnnotateFilePaths(v3);
    }

    /// <summary>
    ///     Finds patterns that look like file paths (e.g. "src/Foo.cs", "path/to/bar.ts")
    ///     inside code blocks and ensures they have a language-annotated fence.
    ///     Also wraps bare path mentions in the workspace/history sections with a
    ///     short inline label so DeepSeek Coder can resolve them more reliably.
    /// </summary>
    private static string AnnotateFilePaths(string text)
    {
        // Annotate fenced code blocks that are missing a language identifier
        // by inferring it from the file extension of the nearest path mention.
        text = Regex.Replace(
            text,
            @"```\s*\n((?:(?!```).)*?)```",
            m =>
            {
                string inner = m.Groups[1].Value;
                string langHint = InferLanguage(inner);
                return $"```{langHint}\n{inner}```";
            },
            RegexOptions.Singleline);

        return text;
    }

    private static readonly (string Extension, string Language)[] _extensionLanguageMap =
    [
        (".cs", "csharp"),
        (".ts", "typescript"),
        (".js", "javascript"),
        (".py", "python"),
        (".go", "go"),
        (".rs", "rust"),
        (".java", "java"),
        (".json", "json"),
        (".yaml", "yaml"),
        (".yml", "yaml"),
        (".md", "markdown"),
        (".sql", "sql"),
        (".sh", "bash")
    ];

    private static string InferLanguage(string codeBlock)
    {
        // Look for a path comment or extension hint in the first few lines.
        // NOTE: this previously uppercased each line into a variable misleadingly named
        // "lower" and then compared it against lowercase literals with
        // StringComparison.Ordinal — an uppercase-vs-lowercase mismatch that made every
        // comparison silently always false, so this method could never actually return a
        // language hint. Fixed by comparing case-insensitively against the original line
        // instead of pre-uppercasing it.
        IEnumerable<string> firstLines = codeBlock.Split('\n').Take(3);
        foreach (string line in firstLines)
        {
            foreach ((string extension, string language) in _extensionLanguageMap)
            {
                if (line.Contains(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return language;
                }
            }
        }

        return ""; // leave blank if unknown
    }
}
