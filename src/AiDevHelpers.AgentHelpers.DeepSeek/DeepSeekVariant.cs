namespace AiDevHelpers.AgentHelpers.DeepSeek;

/// <summary>
///     Distinguishes the three DeepSeek model lines, each with fundamentally
///     different prompting requirements.
/// </summary>
public enum DeepSeekVariant
{
    /// <summary>
    ///     DeepSeek R1 — reasoning model with built-in chain-of-thought.
    ///     Critical differences from other variants:
    ///     - System prompt must be EMPTY (non-empty system prompts hurt benchmark scores)
    ///     - All context is prepended to the USER message
    ///     - No "think step by step" instructions (model deliberates internally)
    ///     - No few-shot examples (conflict with internal reasoning chain)
    ///     - Keep each turn minimal and self-contained
    /// </summary>
    R1,

    /// <summary>
    ///     DeepSeek V3 / V3.1 — chat model.
    ///     Supports system prompts, function calling, JSON mode.
    ///     Loves structure: Markdown headers, bullet points, XML tags all improve adherence.
    /// </summary>
    V3,

    /// <summary>
    ///     DeepSeek Coder — code-specialised chat model.
    ///     Treat like V3 but annotate each file block with language and path.
    ///     Fenced code blocks with path annotations significantly improve output quality.
    /// </summary>
    Coder
}
