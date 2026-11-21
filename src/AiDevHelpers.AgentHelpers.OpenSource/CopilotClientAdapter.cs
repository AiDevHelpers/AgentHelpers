using AiDevHelpers.AgentHelpers.Core.Contracts;
using AiDevHelpers.AgentHelpers.Core.Models;
using AiDevHelpers.AgentHelpers.Core.Services;

namespace AiDevHelpers.AgentHelpers.OpenSource;

/// <summary>
///     Adapts context for GitHub Copilot (VS Code / Copilot Chat / Copilot CLI).
///     Detection: <c>ClientName</c> matches "copilot", "github-copilot", "vscode-copilot",
///     or "github.copilot" from the MCP initialize handshake.
///     Key behaviour differences from generic agents:
///     - Copilot silently drops content above ~8 K tokens per tool response by relevance
///     priority — it does NOT ask for missing content.
///     - Reserves ~30–35 % of its context window as output buffer; effective context is
///     ~130 K on a 192 K window but only ~25 K reaches us via an MCP response cap.
///     - Always label summaries explicitly; never send a partial file without a
///     [SUMMARY — N lines] header.
/// </summary>
public sealed class CopilotClientAdapter : IAgentAdapter
{
    private static readonly HashSet<string> KnownClientNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "copilot", "github-copilot", "vscode-copilot", "github.copilot",
        "github copilot", "copilot-chat"
    };

    public string Family => "Copilot";
    public int Priority => 90;
    public AdapterKind Kind => AdapterKind.Client;

    public bool CanHandle(CallerInfo caller)
    {
        return !string.IsNullOrEmpty(caller.ClientName) && KnownClientNames.Contains(caller.ClientName);
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        return ContextFormat.Copilot;
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        // Copilot reads Markdown well; prepend a brief orientation note so it
        // understands the response came from a tokensaver MCP tool.
        string header =
            "## TokenSaver Context\n" +
            "> ⚠ Content below was built by the TokenSaver MCP server.\n" +
            "> Summaries are labelled [SUMMARY]. Full files were included where the\n" +
            "> per-file limit allowed. Copilot will NOT auto-fetch missing detail —\n" +
            "> request specific files explicitly if you need more.\n\n";

        string trimmed = TokenEstimator.TrimToBudget(
            rawContext, format.ContextTokenBudget);

        return header + trimmed;
    }
}
