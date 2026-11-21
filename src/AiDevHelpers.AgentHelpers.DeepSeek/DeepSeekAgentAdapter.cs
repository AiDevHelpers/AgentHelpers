using AiDevHelpers.AgentHelpers.Core.Contracts;
using AiDevHelpers.AgentHelpers.Core.Models;

namespace AiDevHelpers.AgentHelpers.DeepSeek;

/// <summary>
///     Adapts concise context for DeepSeek Coder, R1, and V3 model lines.
///     Detection: matches when the model hint contains "deepseek".
///     Formatting strategy varies sharply by variant:
///     R1 — reasoning model:
///     • System prompt set to empty string (non-empty prompts degrade R1 performance)
///     • Entire context prepended to the user message body
///     • No chain-of-thought instructions, no few-shot examples
///     • Minimal, direct formatting — no decorative structure
///     V3 — chat model:
///     • Short role statement in system prompt
///     • XML-wrapped sections in body (workspace, files, instructions)
///     • Markdown headers within sections for readability
///     Coder — code-focused:
///     • Like V3 but each file block annotated with language + path
///     • Fenced code blocks with explicit language identifiers
/// </summary>
public sealed class DeepSeekAgentAdapter : IAgentAdapter
{
    public string Family => "DeepSeek";
    public int Priority => 80;
    public AdapterKind Kind => AdapterKind.Model;

    public bool CanHandle(CallerInfo caller)
    {
        return (caller.ModelHint ?? "").Contains("deepseek", StringComparison.OrdinalIgnoreCase);
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        DeepSeekVariant variant = DetectVariant(caller);
        return variant switch
        {
            DeepSeekVariant.R1 => ContextFormat.DeepSeekR1,
            _ => ContextFormat.DeepSeekV3
        };
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        DeepSeekVariant variant = DetectVariant(caller);
        return DeepSeekContextFormatter.Format(rawContext, variant, format);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    internal static DeepSeekVariant DetectVariant(CallerInfo caller)
    {
        string hint = caller.ModelHint ?? "";

        // R1 family: deepseek-r1, deepseek-r1-distill-*, deepseek-reasoner
        if (hint.Contains("r1", StringComparison.OrdinalIgnoreCase) || hint.Contains("reasoner", StringComparison.OrdinalIgnoreCase))
        {
            return DeepSeekVariant.R1;
        }

        // Coder family: deepseek-coder-*, deepseek-coder-v2-*
        if (hint.Contains("coder", StringComparison.OrdinalIgnoreCase))
        {
            return DeepSeekVariant.Coder;
        }

        // V3 / V3.1 / chat — treat as default
        return DeepSeekVariant.V3;
    }
}
