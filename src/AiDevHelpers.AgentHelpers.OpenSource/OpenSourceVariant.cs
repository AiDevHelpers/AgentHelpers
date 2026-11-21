namespace AiDevHelpers.AgentHelpers.OpenSource;

/// <summary>
///     Distinguishes the three open-source model families covered by this adapter.
///     Each has meaningfully different chat-template conventions and prompting preferences.
/// </summary>
public enum OpenSourceVariant
{
    /// <summary>
    ///     Meta Llama 3.x series.
    ///     Chat template uses header/eot tokens handled by Ollama automatically.
    ///     Our job is content formatting: single system message + Markdown headers.
    ///     Context window typically 8K–128K depending on build; budget conservatively at 32K.
    /// </summary>
    Llama,

    /// <summary>
    ///     Mistral 7B, Mistral NeMo, Mixtral 8x7B / 8x22B.
    ///     Less restrictive than Llama; follows direct instructions reliably.
    ///     Prefers short, focused system prompts (one sentence) and bullet-point
    ///     structure inside the context body.
    /// </summary>
    Mistral,

    /// <summary>
    ///     Google Gemma 2 / Gemma 3.
    ///     No separate system message — the template embeds system content into
    ///     the first user turn (strict user/assistant alternation required).
    ///     ContextFormat.SupportsSystemPrompt = false; content folded into user block.
    /// </summary>
    Gemma
}
