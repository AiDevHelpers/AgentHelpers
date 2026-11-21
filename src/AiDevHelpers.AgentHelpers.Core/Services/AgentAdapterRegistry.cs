using AiDevHelpers.AgentHelpers.Core.Contracts;
using AiDevHelpers.AgentHelpers.Core.Models;

using Microsoft.Extensions.Logging;

namespace AiDevHelpers.AgentHelpers.Core.Services;

/// <summary>
///     Holds all registered <see cref="IAgentAdapter" /> instances and resolves
///     the best one for a given <see cref="CallerInfo" />.
///     Adapters are evaluated in descending <see cref="IAgentAdapter.Priority" /> order.
///     The first adapter whose <see cref="IAgentAdapter.CanHandle" /> returns true wins.
///     If no adapter matches, <see cref="DefaultAdapter" /> is returned.
/// </summary>
public sealed partial class AgentAdapterRegistry
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "AgentAdapter resolved: {Family} (priority {Priority}) for client '{Client}' model '{Model}'")]
    private static partial void LogAdapterResolved(ILogger logger, string family, int priority, string client, string model);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No adapter matched client '{Client}' model '{Model}'; using default")]
    private static partial void LogNoAdapterMatched(ILogger logger, string client, string model);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "AgentAdapter resolved — model: {Model}, client: {Client} " +
                   "for caller '{CallerClient}' / model '{CallerModel}'")]
    private static partial void LogBothAdaptersResolved(ILogger logger, string model, string client, string callerClient, string callerModel);

    private readonly IReadOnlyList<IAgentAdapter> _adapters;
    private readonly ILogger<AgentAdapterRegistry> _logger;

    public AgentAdapterRegistry(
        IEnumerable<IAgentAdapter> adapters,
        ILogger<AgentAdapterRegistry> logger)
    {
        _logger = logger;
        _adapters = adapters.OrderByDescending(a => a.Priority).ToList();
        DefaultAdapter = new DefaultAgentAdapter();
    }

    public IAgentAdapter DefaultAdapter { get; }

    /// <summary>
    ///     Returns the highest-priority adapter that can handle <paramref name="caller" />,
    ///     or the built-in <see cref="DefaultAgentAdapter" /> when nothing matches.
    ///     Prefers model adapters; use <see cref="ResolveBoth" /> to get both simultaneously.
    /// </summary>
    public IAgentAdapter Resolve(CallerInfo caller)
    {
        foreach (IAgentAdapter adapter in _adapters)
        {
            if (adapter.CanHandle(caller))
            {
                LogAdapterResolved(_logger, adapter.Family, adapter.Priority, caller.ClientName, caller.ModelHint ?? "(none)");
                return adapter;
            }
        }

        LogNoAdapterMatched(_logger, caller.ClientName, caller.ModelHint ?? "(none)");
        return DefaultAdapter;
    }

    /// <summary>
    ///     Resolves two adapters simultaneously — one for the calling client application
    ///     (determines <see cref="ContextFormat.WillAskForMissingContext" />,
    ///     <see cref="ContextFormat.PerFileTokenLimit" />) and one for the AI model
    ///     (determines formatting, system-prompt support, CoT behaviour).
    ///     Merge the formats with <see cref="ContextFormat.WithClientOverrides" /> to get
    ///     a combined format, then use the model adapter's
    ///     <see cref="IAgentAdapter.AdaptContext" /> for final rendering.
    /// </summary>
    public (IAgentAdapter model, IAgentAdapter client) ResolveBoth(CallerInfo caller)
    {
        IAgentAdapter? modelAdapter = null;
        IAgentAdapter? clientAdapter = null;

        foreach (IAgentAdapter adapter in _adapters)
        {
            if (!adapter.CanHandle(caller))
            {
                continue;
            }

            if (adapter.Kind == AdapterKind.Model && modelAdapter == null)
            {
                modelAdapter = adapter;
            }

            if (adapter.Kind == AdapterKind.Client && clientAdapter == null)
            {
                clientAdapter = adapter;
            }

            if (modelAdapter != null && clientAdapter != null)
            {
                break;
            }
        }

        modelAdapter ??= DefaultAdapter;
        // Default is a no-op client: WillAskForMissingContext=true, PerFileTokenLimit=0
        clientAdapter ??= DefaultAdapter;

        LogBothAdaptersResolved(_logger, modelAdapter.Family, clientAdapter.Family, caller.ClientName, caller.ModelHint ?? "(none)");

        return (modelAdapter, clientAdapter);
    }
}
