using TokenSaver.AgentHelpers.Core.Contracts;
using TokenSaver.AgentHelpers.Core.Models;
using TokenSaver.AgentHelpers.Core.Services;

namespace TokenSaver.AgentHelpers.OpenSource;

/// <summary>
///     Adapts context for Continue.dev (VS Code / JetBrains extension).
///     Detection: <c>ClientName</c> matches "continue" from the MCP initialize handshake.
///     Key behaviour differences:
///     - Continue errors when a file context provider returns content above ~12 K tokens
///     (~400 lines).  <see cref="ContextFormat.PerFileTokenLimit" /> is set to 10 K to
///     stay safely below this threshold.
///     - Continue will NOT ask for missing content if a file was truncated; it treats the
///     received content as complete.
///     - Underlying model varies (user-configured); we use a safe, generic Markdown format.
/// </summary>
public sealed class ContinueClientAdapter : IAgentAdapter
{
    private static readonly HashSet<string> KnownClientNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "continue", "continue.dev", "continuedev"
    };

    public string Family => "Continue";
    public int Priority => 90;
    public AdapterKind Kind => AdapterKind.Client;

    public bool CanHandle(CallerInfo caller)
    {
        return !string.IsNullOrEmpty(caller.ClientName) && KnownClientNames.Contains(caller.ClientName);
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        return ContextFormat.Continue;
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        string header =
            "## TokenSaver Context\n" +
            "> ⚠ Files above 10 K tokens were split or truncated (Continue.dev hard limit).\n" +
            "> Summaries are labelled [SUMMARY — N lines]. Continue will NOT auto-fetch\n" +
            "> missing content — request specific files explicitly if you need more.\n\n";

        string trimmed = TokenEstimator.TrimToBudget(
            rawContext, format.ContextTokenBudget);

        return header + trimmed;
    }
}
