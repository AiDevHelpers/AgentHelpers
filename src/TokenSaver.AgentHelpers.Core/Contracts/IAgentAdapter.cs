using TokenSaver.AgentHelpers.Core.Models;

namespace TokenSaver.AgentHelpers.Core.Contracts;

/// <summary>
///     Classifies an <see cref="IAgentAdapter" /> as detecting the calling client application
///     (Cursor, Copilot, Continue — determines behavioural flags like
///     <see cref="ContextFormat.WillAskForMissingContext" />) versus the underlying AI model
///     (Claude, Qwen, DeepSeek — determines prompt formatting).
///     Both kinds can match simultaneously; <see cref="AgentAdapterRegistry.ResolveBoth" />
///     returns one of each so their <see cref="ContextFormat" />s can be merged.
/// </summary>
public enum AdapterKind
{
    /// <summary>Detects the AI model family (formatting, system-prompt support, CoT).</summary>
    Model,

    /// <summary>Detects the client application (WillAskForMissingContext, PerFileTokenLimit).</summary>
    Client
}

/// <summary>
///     Adapts raw concise context for a specific AI model family or client application.
///     Implementations are registered via
///     <see cref="TokenSaver.AgentHelpers.Core.Extensions.AgentHelpersServiceExtensions.AddAdapter{TAdapter}" />
///     and resolved through <see cref="TokenSaver.AgentHelpers.Core.Services.AgentAdapterRegistry" />.
/// </summary>
public interface IAgentAdapter
{
    /// <summary>Human-readable family name (e.g. "Anthropic", "Qwen", "Cursor").</summary>
    string Family { get; }

    /// <summary>
    ///     Resolution priority — higher wins when multiple adapters of the same
    ///     <see cref="Kind" /> match the same <see cref="CallerInfo" />.
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     Whether this adapter detects the client application or the AI model.
    ///     Used by <see cref="AgentAdapterRegistry.ResolveBoth" /> to separate the two lanes.
    /// </summary>
    AdapterKind Kind { get; }

    /// <summary>Returns true if this adapter can handle the identified caller.</summary>
    bool CanHandle(CallerInfo caller);

    /// <summary>Returns the <see cref="ContextFormat" /> best suited for this caller.</summary>
    ContextFormat GetFormat(CallerInfo caller);

    /// <summary>
    ///     Transforms <paramref name="rawContext" /> (the joined output of <c>LocalContextService</c>)
    ///     into a form optimally structured for the calling AI.
    /// </summary>
    string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format);
}
