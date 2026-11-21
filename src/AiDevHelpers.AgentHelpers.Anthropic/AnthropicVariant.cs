namespace AiDevHelpers.AgentHelpers.Anthropic;

/// <summary>
///     Distinguishes Claude model tiers within the Anthropic family.
///     Each variant gets a different <c>ContextFormat</c> (token budget, trimming aggressiveness)
///     and may receive extra blocks in the adapted context.
/// </summary>
public enum AnthropicVariant
{
    /// <summary>Claude Sonnet — balanced capability and speed. Default Anthropic variant.</summary>
    Sonnet,

    /// <summary>
    ///     Claude Opus — highest capability, larger context budget.
    ///     Receives an additional &lt;guidance&gt; block with the agent type hint.
    /// </summary>
    Opus,

    /// <summary>
    ///     Claude Haiku — fastest and most token-efficient.
    ///     Context is aggressively trimmed: comments stripped, blank lines collapsed.
    /// </summary>
    Haiku
}
