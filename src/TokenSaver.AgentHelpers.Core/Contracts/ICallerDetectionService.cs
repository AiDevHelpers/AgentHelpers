using TokenSaver.AgentHelpers.Core.Models;

using Microsoft.AspNetCore.Http;

namespace TokenSaver.AgentHelpers.Core.Contracts;

/// <summary>
///     Detects the calling AI client and model from an inbound MCP request.
///     Detection waterfall (first match wins):
///     1. mcpClientName / mcpClientVersion — from IMcpServer.ClientInfo, extracted by the caller
///     before calling Detect (avoids a hard reference to the MCP package in this library).
///     2. X-Model-Hint or X-LLM-Model request header — explicit model name set by the client.
///     3. User-Agent request header — last-resort string match.
///     4. TokenSaverConfig.OllamaModel — used when our own Ollama stack is the caller.
///     5. Safe-default <see cref="CallerInfo" /> with empty strings.
///     Call this on every tool invocation; detection is pure string matching with no I/O.
/// </summary>
public interface ICallerDetectionService
{
    /// <summary>
    ///     Detects and returns a <see cref="CallerInfo" /> for the current request.
    ///     Never throws; returns a default <see cref="CallerInfo" /> on any failure.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="mcpClientName">
    ///     Value of <c>IMcpServer.ClientInfo?.Name</c>, if available.
    ///     Pass null when not available (stateless transport with no prior handshake).
    /// </param>
    /// <param name="mcpClientVersion">
    ///     Value of <c>IMcpServer.ClientInfo?.Version</c>, if available.
    /// </param>
    CallerInfo Detect(
        HttpContext httpContext,
        string? mcpClientName = null,
        string? mcpClientVersion = null);
}
