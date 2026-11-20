using TokenSaver.AgentHelpers.Core.Contracts;
using TokenSaver.AgentHelpers.Core.Models;

namespace TokenSaver.AgentHelpers.Core.Services;

/// <summary>
///     Fallback adapter used when no specific adapter matches the caller.
///     Returns the safe-default <see cref="ContextFormat" /> and passes context through unchanged.
/// </summary>
internal sealed class DefaultAgentAdapter : IAgentAdapter
{
    public string Family => "Default";
    public AdapterKind Kind => AdapterKind.Model;
    public int Priority => int.MinValue;

    public bool CanHandle(CallerInfo caller)
    {
        return true;
        // always matches as last resort
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        return ContextFormat.Default;
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        return TokenEstimator.TrimToBudget(rawContext, format.ContextTokenBudget);
    }
}
