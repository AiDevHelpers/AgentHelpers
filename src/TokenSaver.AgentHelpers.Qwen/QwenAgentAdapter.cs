using TokenSaver.AgentHelpers.Core.Contracts;
using TokenSaver.AgentHelpers.Core.Models;

namespace TokenSaver.AgentHelpers.Qwen;

/// <summary>
///     Adapts concise context for the Qwen 2.5 and Qwen 3.x model families.
///     Detection: matches when the model hint contains "qwen" or "qwq".
///     Formatting strategy:
///     - Markdown ## headers for section separation
///     - Role-priming sentence at the top of the system prompt
///     - Output schema block appended (reduces format drift ~60%)
///     - Self-check instruction appended (catches ~1 in 3 hallucinations)
///     - Chain-of-thought instructions suppressed for Qwen3/thinking variants
///     (they deliberate internally; external CoT prompts conflict)
/// </summary>
public sealed class QwenAgentAdapter : IAgentAdapter
{
    private static readonly string[] ModelTokens = ["qwen", "qwq"];
    public string Family => "Qwen";
    public int Priority => 80;
    public AdapterKind Kind => AdapterKind.Model;

    public bool CanHandle(CallerInfo caller)
    {
        string hint = caller.ModelHint ?? "";
        return ModelTokens.Any(t => hint.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        QwenVariant variant = DetectVariant(caller);

        // Qwen3-thinking / Plus models can handle very large contexts
        int budget = variant == QwenVariant.Qwen3Thinking ? 500_000 : 100_000;

        return ContextFormat.Qwen with { ContextTokenBudget = budget };
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        QwenVariant variant = DetectVariant(caller);
        return QwenContextFormatter.Format(rawContext, variant, format);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    internal static QwenVariant DetectVariant(CallerInfo caller)
    {
        // Compare case-insensitively: the hint is matched against lowercase tokens below.
        string hint = caller.ModelHint ?? "";

        // QwQ and explicit thinking / plus variants
        if (hint.Contains("qwq", StringComparison.OrdinalIgnoreCase) ||
            hint.Contains("thinking", StringComparison.OrdinalIgnoreCase) ||
            hint.Contains("plus", StringComparison.OrdinalIgnoreCase) ||
            hint.Contains("qwen3.5", StringComparison.OrdinalIgnoreCase))
        {
            return QwenVariant.Qwen3Thinking;
        }

        // Qwen 3.x standard
        if (hint.Contains("qwen3", StringComparison.OrdinalIgnoreCase))
        {
            return QwenVariant.Qwen3;
        }

        // Qwen 2.x (including 2.5-coder, 2.5-instruct, etc.)
        return QwenVariant.Qwen2;
    }
}
