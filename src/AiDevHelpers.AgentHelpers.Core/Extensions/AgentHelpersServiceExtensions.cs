using AiDevHelpers.AgentHelpers.Core.Contracts;
using AiDevHelpers.AgentHelpers.Core.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiDevHelpers.AgentHelpers.Core.Extensions;

/// <summary>
///     DI registration helpers for AiDevHelpers.AgentHelpers.
/// </summary>
public static class AgentHelpersServiceExtensions
{
    /// <summary>
    ///     Registers AgentHelpers core services.
    ///     <paramref name="ollamaModelName" /> should be the value of
    ///     <c>TokenSaverConfig.OllamaModel</c> — it is used as the lowest-priority
    ///     caller detection signal when no MCP ClientInfo or headers are present.
    ///     Usage in Program.cs:
    ///     <code>
    /// builder.Services
    ///     .AddAgentHelpers(config.OllamaModel)
    ///     .AddAdapter&lt;AnthropicAgentAdapter&gt;()
    ///     .AddAdapter&lt;QwenAgentAdapter&gt;()
    ///     .AddAdapter&lt;DeepSeekAgentAdapter&gt;()
    ///     .AddAdapter&lt;OpenSourceAgentAdapter&gt;();
    /// </code>
    /// </summary>
    public static IServiceCollection AddAgentHelpers(
        this IServiceCollection services,
        string ollamaModelName = "")
    {
        _ = services.AddSingleton<ICallerDetectionService>(sp =>
        {
            ILogger<CallerDetectionService> logger = sp.GetRequiredService<ILogger<CallerDetectionService>>();
            return new CallerDetectionService(ollamaModelName, logger);
        });

        _ = services.AddSingleton<AgentAdapterRegistry>();

        return services;
    }

    /// <summary>
    ///     Registers a concrete <see cref="IAgentAdapter" /> implementation.
    ///     Chain multiple calls to register all adapter libraries.
    /// </summary>
    public static IServiceCollection AddAdapter<TAdapter>(this IServiceCollection services)
        where TAdapter : class, IAgentAdapter
    {
        _ = services.AddSingleton<IAgentAdapter, TAdapter>();
        return services;
    }
}
