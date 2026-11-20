using TokenSaver.AgentHelpers.Core.Models;
using TokenSaver.AgentHelpers.Core.Services;

using System.Text;

namespace TokenSaver.AgentHelpers.Qwen;

/// <summary>
///     Formats raw concise context for Qwen models using Markdown section headers.
///     Output structure:
///     {role-priming line}
///     ## Workspace Summary          — if present
///     {summary}
///     ## Requested Files            — if present
///     {file list}
///     ## Conversation History       — if present
///     {history entries, separated by ---}
///     ## Current Request
///     {prompt}
///     ## Expected Output            — always (AddOutputSchema)
///     {output type hint}
///     ## Verification               — always (AddSelfCheck)
///     {self-check instruction}
///     Chain-of-thought instructions are omitted for Qwen3 and Qwen3Thinking variants.
/// </summary>
internal static class QwenContextFormatter
{
    public static string Format(string rawContext, QwenVariant variant, ContextFormat format)
    {
        ParsedContextParts parts = ContextPartParser.Split(rawContext);
        StringBuilder sb = new();

        // ── Role-priming ──────────────────────────────────────────────────────

        _ = sb.AppendLine(RolePrimingLine(variant));
        _ = sb.AppendLine();

        // ── Workspace summary ─────────────────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(parts.Summary))
        {
            _ = sb.AppendLine("## Workspace Summary");
            _ = sb.AppendLine(parts.Summary.Trim());
            _ = sb.AppendLine();
        }

        // ── Requested files ───────────────────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(parts.RequestedFiles))
        {
            _ = sb.AppendLine("## Requested Files");
            _ = sb.AppendLine(parts.RequestedFiles.Trim());
            _ = sb.AppendLine();
        }

        // ── Conversation history ──────────────────────────────────────────────

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

        // ── Current request ───────────────────────────────────────────────────

        _ = sb.AppendLine("## Current Request");
        _ = sb.AppendLine(parts.Prompt.Trim());
        _ = sb.AppendLine();

        // ── Output schema (AddOutputSchema = true for all Qwen) ───────────────

        if (format.AddOutputSchema)
        {
            _ = sb.AppendLine("## Expected Output");
            _ = sb.AppendLine(OutputSchemaHint(parts.Prompt));
            _ = sb.AppendLine();
        }

        // ── Chain-of-thought (Qwen2 only) ─────────────────────────────────────

        if (variant == QwenVariant.Qwen2 && !format.AvoidChainOfThought)
        {
            _ = sb.AppendLine("Work through the problem step by step before writing your final answer.");
            _ = sb.AppendLine();
        }

        // ── Self-check (AddSelfCheck = true for all Qwen) ────────────────────

        if (format.AddSelfCheck)
        {
            _ = sb.AppendLine("## Verification");
            _ = sb.AppendLine("Before finalising your response, verify each specific claim against " +
                          "the context above. Correct any inaccuracies silently.");
        }

        string result = sb.ToString().TrimEnd();
        return TokenEstimator.TrimToBudget(result, format.ContextTokenBudget);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string RolePrimingLine(QwenVariant variant)
    {
        return variant switch
        {
            QwenVariant.Qwen3Thinking =>
                "You are an expert software engineering assistant with deep reasoning capabilities. " +
                "Apply your full analytical process to the task below.",

            QwenVariant.Qwen3 =>
                "You are an expert software engineering assistant. " +
                "Be precise, thorough, and grounded in the provided context.",

            _ => // Qwen2
                "You are Qwen, an expert software engineering assistant created by Alibaba Cloud. " +
                "Be precise, thorough, and grounded in the provided context."
        };
    }

    /// <summary>
    ///     Infers the most likely output type from the prompt text to seed the output schema hint.
    ///     Falls back to a general "code | explanation | plan | analysis" menu.
    /// </summary>
    private static string OutputSchemaHint(string prompt)
    {
        if (prompt.Contains("fix", StringComparison.OrdinalIgnoreCase) || prompt.Contains("bug", StringComparison.OrdinalIgnoreCase) || prompt.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            prompt.Contains("implement", StringComparison.OrdinalIgnoreCase) || prompt.Contains("write", StringComparison.OrdinalIgnoreCase) || prompt.Contains("add", StringComparison.OrdinalIgnoreCase))
        {
            return "Provide working code with a brief explanation of the changes made.";
        }

        if (prompt.Contains("explain", StringComparison.OrdinalIgnoreCase) || prompt.Contains("what", StringComparison.OrdinalIgnoreCase) || prompt.Contains("how", StringComparison.OrdinalIgnoreCase) ||
            prompt.Contains("why", StringComparison.OrdinalIgnoreCase) || prompt.Contains("describe", StringComparison.OrdinalIgnoreCase))
        {
            return "Provide a clear explanation grounded in the context above.";
        }

        if (prompt.Contains("plan", StringComparison.OrdinalIgnoreCase) || prompt.Contains("design", StringComparison.OrdinalIgnoreCase) || prompt.Contains("architect", StringComparison.OrdinalIgnoreCase) ||
            prompt.Contains("structure", StringComparison.OrdinalIgnoreCase))
        {
            return "Provide a structured plan with numbered steps and rationale.";
        }

        if (prompt.Contains("review", StringComparison.OrdinalIgnoreCase) || prompt.Contains("analyse", StringComparison.OrdinalIgnoreCase) || prompt.Contains("analyze", StringComparison.OrdinalIgnoreCase) ||
            prompt.Contains("check", StringComparison.OrdinalIgnoreCase) || prompt.Contains("audit", StringComparison.OrdinalIgnoreCase))
        {
            return "Provide a structured analysis listing findings and recommendations.";
        }

        // General fallback
        return "Respond with one of: code with explanation | clear explanation | numbered plan | structured analysis.";
    }
}
