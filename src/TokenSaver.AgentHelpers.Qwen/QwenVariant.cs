namespace TokenSaver.AgentHelpers.Qwen;

/// <summary>
///     Distinguishes Qwen model generations and capabilities.
///     Variant affects token budget, whether CoT instructions are suppressed,
///     and whether a default system-prompt identity line is prepended.
/// </summary>
public enum QwenVariant
{
    /// <summary>
    ///     Qwen 2.5 series (qwen2.5-*).
    ///     Expects a system prompt; benefits from explicit role-priming.
    ///     Context window up to 128K.
    /// </summary>
    Qwen2,

    /// <summary>
    ///     Qwen 3.x series (qwen3-*, qwen3.5-*) — standard (non-thinking) mode.
    ///     Ships without a default system prompt; one is provided anyway for focus.
    ///     Does NOT receive chain-of-thought instructions.
    /// </summary>
    Qwen3,

    /// <summary>
    ///     Qwen 3.x "thinking" or "plus" variants (qwen3.*-thinking, qwq-*, qwen3.5-plus).
    ///     Internal reasoning active; CoT instructions suppressed.
    ///     Context window can be very large (up to 1M tokens for Plus).
    /// </summary>
    Qwen3Thinking
}
