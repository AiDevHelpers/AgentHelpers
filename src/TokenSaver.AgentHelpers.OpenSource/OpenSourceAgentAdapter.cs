using TokenSaver.AgentHelpers.Core.Contracts;
using TokenSaver.AgentHelpers.Core.Models;

namespace TokenSaver.AgentHelpers.OpenSource;

/// <summary>
///     Adapts concise context for Llama 3, Mistral / Mixtral, and Gemma model families.
///     Detection: matches when the model hint contains "llama", "mistral", "mixtral", or "gemma".
///     Formatting strategy by variant:
///     Llama:
///     - Single system message with 2–3 sentence role framing
///     - Markdown ## headers for context sections in the user turn
///     - Conservative 32K token budget
///     Mistral:
///     - One-sentence system prompt (more = worse for Mistral)
///     - Bullet-point lists inside context sections (improves instruction-following)
///     - Same conservative 32K budget
///     Gemma:
///     - No system message (SupportsSystemPrompt = false)
///     - System role text prepended to the first user turn, separated by "---"
///     - Format: "You are a coding assistant.\n---\n## Workspace\n…\n\n{prompt}"
/// </summary>
public sealed class OpenSourceAgentAdapter : IAgentAdapter
{
    private static readonly string[] ModelTokens = ["llama", "mistral", "mixtral", "gemma"];
    public string Family => "OpenSource";
    public int Priority => 60;
    public AdapterKind Kind => AdapterKind.Model;

    public bool CanHandle(CallerInfo caller)
    {
        string hint = caller.ModelHint ?? "";
        return ModelTokens.Any(t => hint.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        OpenSourceVariant variant = DetectVariant(caller);
        return variant switch
        {
            OpenSourceVariant.Gemma => ContextFormat.Gemma,
            OpenSourceVariant.Mistral => ContextFormat.Mistral,
            _ => ContextFormat.Llama
        };
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        OpenSourceVariant variant = DetectVariant(caller);
        return OpenSourceContextFormatter.Format(rawContext, variant, format);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    internal static OpenSourceVariant DetectVariant(CallerInfo caller)
    {
        string hint = (caller.ModelHint ?? "").ToUpperInvariant();

        return hint.Contains("gemma", StringComparison.Ordinal)
            ? OpenSourceVariant.Gemma
            : hint.Contains("mistral", StringComparison.Ordinal) || hint.Contains("mixtral", StringComparison.Ordinal)
            ? OpenSourceVariant.Mistral
            : OpenSourceVariant.Llama;
    }
}
