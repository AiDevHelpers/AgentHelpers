using TokenSaver.AgentHelpers.Core.Contracts;
using TokenSaver.AgentHelpers.Core.Models;
using TokenSaver.AgentHelpers.Core.Services;

namespace TokenSaver.AgentHelpers.OpenSource;

/// <summary>
///     Adapts context for Cursor IDE.
///     Detection: <c>ClientName</c> matches "cursor" from the MCP initialize handshake.
///     Key behaviour differences:
///     - Cursor WILL re-read files independently, writing large tool output to temp files
///     and seeking selectively.  <see cref="ContextFormat.WillAskForMissingContext" /> = true.
///     - After its context window fills, Cursor does a lossy summarisation pass — crucial
///     details can be lost.  Prefer fewer, complete files over many shallow summaries.
///     - Cursor only has access to a file outline + small cursor-region for large files;
///     always include full content for files the user is actively editing.
///     - Underlying model varies; use Markdown headers (works universally).
/// </summary>
public sealed class CursorClientAdapter : IAgentAdapter
{
    private static readonly HashSet<string> KnownClientNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cursor", "cursor-ide"
    };

    public string Family => "Cursor";
    public int Priority => 90;
    public AdapterKind Kind => AdapterKind.Client;

    public bool CanHandle(CallerInfo caller)
    {
        return !string.IsNullOrEmpty(caller.ClientName) && KnownClientNames.Contains(caller.ClientName);
    }

    public ContextFormat GetFormat(CallerInfo caller)
    {
        return ContextFormat.Cursor;
    }

    public string AdaptContext(string rawContext, CallerInfo caller, ContextFormat format)
    {
        // Cursor handles Markdown well; add a note about summary labelling so that if
        // Cursor's own file reader produces different content, it knows a summary was sent.
        string header =
            "## TokenSaver Context\n" +
            "> Summaries are labelled [SUMMARY — N lines, full file at path].\n" +
            "> Cursor can re-read any file listed below for full content.\n" +
            "> Prefer full reads over these summaries for files you are actively editing,\n" +
            "> as Cursor's context-fill summarisation may lose detail from summaries.\n\n";

        string trimmed = TokenEstimator.TrimToBudget(
            rawContext, format.ContextTokenBudget);

        return header + trimmed;
    }
}
