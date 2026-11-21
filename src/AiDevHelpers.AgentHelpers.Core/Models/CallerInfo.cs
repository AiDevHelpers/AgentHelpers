namespace AiDevHelpers.AgentHelpers.Core.Models;

/// <summary>
///     Identifies the AI client and model that sent the current MCP request.
///     Populated by <see cref="Services.CallerDetectionService" /> from a waterfall of signals:
///     1. IMcpServer.ClientInfo (from MCP initialize handshake, may be null in stateless mode)
///     2. X-Model-Hint / X-LLM-Model request header
///     3. User-Agent request header
///     4. TokenSaverConfig.OllamaModel (when our own Ollama stack is the caller)
///     5. Safe-default fallback
/// </summary>
public record CallerInfo
{
    /// <summary>
    ///     Client application name from the MCP initialize handshake, e.g. "claude-code", "cursor".
    /// </summary>
    public string ClientName { get; init; } = "";

    /// <summary>
    ///     Client application version from the MCP initialize handshake.
    /// </summary>
    public string? ClientVersion { get; init; }

    /// <summary>
    ///     Explicit model name hint — from X-Model-Hint header, X-LLM-Model header,
    ///     or TokenSaverConfig.OllamaModel.  Lower-cased before storage.
    ///     Examples: "claude-sonnet-4-6", "deepseek-r1", "qwen2.5-coder", "llama3.2"
    /// </summary>
    public string? ModelHint { get; init; }

    /// <summary>
    ///     Raw User-Agent header value. Used as a last-resort signal.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    ///     Which detection signal actually produced this CallerInfo.
    ///     Useful for logging and diagnostics.
    /// </summary>
    public DetectionSource Source { get; init; } = DetectionSource.Default;
}

public enum DetectionSource
{
    /// <summary>Identified from IMcpServer.ClientInfo (MCP initialize handshake).</summary>
    McpClientInfo,

    /// <summary>Identified from X-Model-Hint or X-LLM-Model request header.</summary>
    ModelHintHeader,

    /// <summary>Identified from User-Agent header.</summary>
    UserAgent,

    /// <summary>Identified from TokenSaverConfig.OllamaModel.</summary>
    OllamaConfig,

    /// <summary>No signal matched; using safe defaults.</summary>
    Default
}
