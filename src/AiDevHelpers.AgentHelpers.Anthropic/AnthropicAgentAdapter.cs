using AiDevHelpers.AgentHelpers.Core.Contracts;
using AiDevHelpers.AgentHelpers.Core.Models;

namespace AiDevHelpers.AgentHelpers.Anthropic;

/// <summary>
///     Adapts concise context for Claude Sonnet, Opus, and Haiku.
///     Detection: matches when ClientInfo.Name is "claude-code" or "claude-ai",
///     or when the model hint contains "claude", "sonnet", "opus", or "haiku".
///     Formatting strategy:
///     - Wraps sections in XML tags for structured context parsing
///     - Orders stable content first (summary → files) for prompt-cache eligibility
///     - Haiku: aggressive trimming, no decoration
///     - Opus: larger token budget, adds a &lt;guidance&gt; block
/// </summary>
public sealed class AnthropicAgentAdapter : IAgentAdapter
{
    // Client names sent by Claude products in the MCP initialize handshake
    private static readonly HashSet<string> KnownClientNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "claude-code", "claude-ai", "claude", "claude-desktop", "cowork"
    };

    // Model name substrings that identify any Claude model
    private static readonly string[] ModelTokens = ["claude", "sonnet", "opus", "haiku"];
    public string Family => "Anthropic";
    public int Priority => 100;
    public AdapterKind Kind => AdapterKind.Model;

    public bool CanHandle(CallerInfo caller)
    {
        if (!string.IsNullOrEmpty(caller.ClientName) && KnownClientNames.Contains(caller.ClientName))
        {
            return true;
        }

        string hint = caller.ModelHint ?? "";
        return ModelTokens.Any(t => hint.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        AnthropicVariant variant = DetectVariant(caller);
        return variant switch
        {
            AnthropicVariant.Haiku => ContextFormat.AnthropicHaiku,
            AnthropicVariant.Opus => ContextFormat.AnthropicOpus,
            _ => ContextFormat.AnthropicSonnet
        };
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        AnthropicVariant variant = DetectVariant(caller);
        return AnthropicContextFormatter.Format(rawContext, variant, format);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    internal static AnthropicVariant DetectVariant(CallerInfo caller)
    {
        string hint = caller.ModelHint ?? "";
        string client = caller.ClientName ?? "";
        string ua = caller.UserAgent ?? "";

        string combined = $"{hint} {client} {ua}";

        if (combined.Contains("haiku", StringComparison.OrdinalIgnoreCase))
        {
            return AnthropicVariant.Haiku;
        }

        if (combined.Contains("opus", StringComparison.OrdinalIgnoreCase))
        {
            return AnthropicVariant.Opus;
        }

        // claude-code defaults to Sonnet in most configurations
        return AnthropicVariant.Sonnet;
    }
}
