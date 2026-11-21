namespace AiDevHelpers.AgentHelpers.Core.Models;

/// <summary>
///     Describes how a specific AI model family prefers to receive context.
///     Produced by <see cref="Contracts.IAgentAdapter.GetFormat" /> and consumed by
///     <see cref="Contracts.IAgentAdapter.AdaptContext" /> and by LocalContextService.
/// </summary>
public record ContextFormat
{
    /// <summary>Safe defaults used when no adapter matches.</summary>
    public static readonly ContextFormat Default = new();

    /// <summary>Format for Anthropic Sonnet — balanced, XML sections, 80K budget.</summary>
    public static readonly ContextFormat AnthropicSonnet = new()
    {
        UseXmlSectionTags = true,
        UseMarkdownHeaders = false,
        SupportsSystemPrompt = true,
        OrderForPromptCache = true,
        ContextTokenBudget = 80_000
    };

    /// <summary>Format for Anthropic Opus — same as Sonnet but larger budget.</summary>
    public static readonly ContextFormat AnthropicOpus = new()
    {
        UseXmlSectionTags = true,
        UseMarkdownHeaders = false,
        SupportsSystemPrompt = true,
        OrderForPromptCache = true,
        ContextTokenBudget = 120_000
    };

    /// <summary>Format for Anthropic Haiku — aggressive trimming, short prompts.</summary>
    public static readonly ContextFormat AnthropicHaiku = new()
    {
        UseXmlSectionTags = true,
        UseMarkdownHeaders = false,
        SupportsSystemPrompt = true,
        OrderForPromptCache = true,
        ShortFocusedPrompts = true,
        ContextTokenBudget = 40_000
    };

    /// <summary>Format for Qwen 2.5 / 3.x — output schema + self-check, Markdown headers.</summary>
    public static readonly ContextFormat Qwen = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = true,
        AddOutputSchema = true,
        AddSelfCheck = true,
        ContextTokenBudget = 100_000
    };

    /// <summary>Format for DeepSeek R1 — no system prompt, minimal, no CoT.</summary>
    public static readonly ContextFormat DeepSeekR1 = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = false,
        SupportsSystemPrompt = false,
        AvoidChainOfThought = true,
        ContextTokenBudget = 60_000
    };

    /// <summary>Format for DeepSeek V3 / Coder — XML + Markdown, system prompt OK.</summary>
    public static readonly ContextFormat DeepSeekV3 = new()
    {
        UseXmlSectionTags = true,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = true,
        ContextTokenBudget = 60_000
    };

    /// <summary>Format for Llama 3 — Markdown headers, standard system prompt, conservative budget.</summary>
    public static readonly ContextFormat Llama = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = true,
        ContextTokenBudget = 32_000
    };

    /// <summary>Format for Mistral — short focused prompts, bullets, conservative budget.</summary>
    public static readonly ContextFormat Mistral = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = true,
        ShortFocusedPrompts = true,
        ContextTokenBudget = 32_000
    };

    /// <summary>Format for Gemma — system context embedded in first user turn, no separate system prompt.</summary>
    public static readonly ContextFormat Gemma = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = false,
        ContextTokenBudget = 32_000
    };

    // ── Client application formats (detected by ClientName, not model) ────────

    /// <summary>
    ///     Format for GitHub Copilot (VS Code extension / Copilot Chat).
    ///     Copilot silently drops content by relevance priority above its output-buffer
    ///     threshold (~8 K tokens per tool response) and will NOT ask for missing content.
    ///     Keep responses short and always label summaries explicitly.
    /// </summary>
    public static readonly ContextFormat Copilot = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = true,
        ShortFocusedPrompts = true,
        WillAskForMissingContext = false,
        PerFileTokenLimit = 8_000,
        ContextTokenBudget = 25_000 // MCP hard cap default
    };

    /// <summary>
    ///     Format for Continue.dev.
    ///     Continue errors when a file context provider returns content above ~12 K tokens
    ///     and will NOT request missing content.  Split or skip large files.
    /// </summary>
    public static readonly ContextFormat Continue = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = true,
        WillAskForMissingContext = false,
        PerFileTokenLimit = 10_000, // stay comfortably under the 12 K hard error
        ContextTokenBudget = 40_000
    };

    /// <summary>
    ///     Format for Cursor.
    ///     Cursor writes large tool output to temp files and re-reads selectively, so it
    ///     CAN discover missing content — but sends a lossy summarisation after context fills.
    ///     Always label summaries; prefer full files over many summaries to avoid
    ///     post-fill degradation.
    /// </summary>
    public static readonly ContextFormat Cursor = new()
    {
        UseXmlSectionTags = false,
        UseMarkdownHeaders = true,
        SupportsSystemPrompt = true,
        WillAskForMissingContext = true,
        PerFileTokenLimit = 0,
        ContextTokenBudget = 60_000
    };
    // ── Structure ─────────────────────────────────────────────────────────────

    /// <summary>
    ///     Wrap major context sections in XML tags, e.g. &lt;workspace_summary&gt;…&lt;/workspace_summary&gt;.
    ///     Best for: Anthropic models, DeepSeek V3/Coder.
    /// </summary>
    public bool UseXmlSectionTags { get; init; }

    /// <summary>
    ///     Use Markdown ## headers to separate context sections.
    ///     Best for: Qwen, Llama, Mistral, Gemma, DeepSeek V3/Coder.
    /// </summary>
    public bool UseMarkdownHeaders { get; init; } = true;

    // ── System Prompt ─────────────────────────────────────────────────────────

    /// <summary>
    ///     True if the model handles a separate system prompt correctly.
    ///     False means context must be folded into the first user message.
    ///     Set to false for: DeepSeek R1, Gemma.
    /// </summary>
    public bool SupportsSystemPrompt { get; init; } = true;

    // ── Reasoning Controls ────────────────────────────────────────────────────

    /// <summary>
    ///     True for reasoning models (DeepSeek R1) that perform internal deliberation.
    ///     Suppresses "think step by step" instructions and few-shot examples, which
    ///     conflict with the model's built-in chain-of-thought process.
    /// </summary>
    public bool AvoidChainOfThought { get; init; }

    // ── Output Quality Boosters ───────────────────────────────────────────────

    /// <summary>
    ///     Append an expected output schema block to the context.
    ///     Reduces format drift by ~60% on Qwen models.
    /// </summary>
    public bool AddOutputSchema { get; init; }

    /// <summary>
    ///     Append a self-check instruction ("verify each claim against the context above").
    ///     Catches ~1 in 3 hallucinated specifics on Qwen models.
    /// </summary>
    public bool AddSelfCheck { get; init; }

    // ── Size / Density ────────────────────────────────────────────────────────

    /// <summary>
    ///     Prefer short, focused system prompts and lean context bodies.
    ///     Enabled for Mistral and Claude Haiku.
    /// </summary>
    public bool ShortFocusedPrompts { get; init; }

    /// <summary>
    ///     Advisory token budget for the total context block.
    ///     CallerDetectionService trims content to stay under this limit.
    ///     Uses chars/4 estimation — see <see cref="Services.TokenEstimator" />.
    /// </summary>
    public int ContextTokenBudget { get; init; } = 60_000;

    // ── Prompt Caching ────────────────────────────────────────────────────────

    /// <summary>
    ///     True for Anthropic models that benefit from putting stable content
    ///     (project summary, file list) first so the prefix is cache-eligible.
    /// </summary>
    public bool OrderForPromptCache { get; init; }

    // ── Context Completeness Behaviour ────────────────────────────────────────

    /// <summary>
    ///     True when the consuming agent will proactively re-read files if it decides
    ///     it needs more detail (Claude Code, Cursor).  False means anything not sent
    ///     in the MCP response is effectively invisible — summaries must be labelled
    ///     and the caller should be offered full file content instead.
    ///     Defaults to true (safe — only opt-out explicitly for agents that won't ask).
    /// </summary>
    public bool WillAskForMissingContext { get; init; } = true;

    /// <summary>
    ///     Hard per-file token cap imposed by the consuming agent before it errors or
    ///     silently truncates.  0 = no enforced limit (rely on ContextTokenBudget only).
    ///     Continue.dev: ~10 000.  Copilot: ~8 000 (silent priority-drop above this).
    /// </summary>
    public int PerFileTokenLimit { get; init; }

    // ── Predefined Formats ────────────────────────────────────────────────────

    // ── Merging ───────────────────────────────────────────────────────────────

    /// <summary>
    ///     Returns a new <see cref="ContextFormat" /> that starts from this instance
    ///     (the model format) and overlays the client-application-specific properties
    ///     from <paramref name="clientFormat" />.
    ///     Client properties: <see cref="WillAskForMissingContext" />, <see cref="PerFileTokenLimit" />.
    ///     All other properties (XML tags, system-prompt support, CoT, token budget, etc.)
    ///     are taken from the model format (<c>this</c>).
    /// </summary>
    public ContextFormat WithClientOverrides(ContextFormat clientFormat)
    {
        return this with
        {
            WillAskForMissingContext = clientFormat.WillAskForMissingContext,
            PerFileTokenLimit = MergePerFileTokenLimit(PerFileTokenLimit, clientFormat.PerFileTokenLimit)
        };
    }

    /// <summary>
    ///     Merges the model's and the client application's PerFileTokenLimit: 0 means "no
    ///     limit imposed" for either side, and when both impose a limit, the stricter (lower)
    ///     one wins.
    /// </summary>
    private static int MergePerFileTokenLimit(int modelLimit, int clientLimit)
    {
        return (modelLimit, clientLimit) switch
        {
            (_, 0) => modelLimit, // Client has no limit — use the model's.
            (0, _) => clientLimit, // Model has no limit — use the client's.
            _ => Math.Min(modelLimit, clientLimit) // Both have limits — take the stricter one.
        };
    }
}
