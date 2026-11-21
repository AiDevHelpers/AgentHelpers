using AiDevHelpers.AgentHelpers.Core.Contracts;
using AiDevHelpers.AgentHelpers.Core.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AiDevHelpers.AgentHelpers.Core.Services;

/// <summary>
///     Detects the calling AI client and model on every MCP tool invocation.
///     Waterfall (first non-empty match wins):
///     1. mcpClientName / mcpClientVersion  (from IMcpServer.ClientInfo, passed in by caller)
///     2. X-Model-Hint header               (explicit client-set model name)
///     3. X-LLM-Model header                (alias)
///     4. User-Agent header                 (last-resort string match)
///     5. OllamaModel from config           (when our own Ollama stack is caller)
///     6. Safe-default empty CallerInfo
/// </summary>
public sealed partial class CallerDetectionService : ICallerDetectionService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Caller detected via MCP ClientInfo: {Name} {Version}")]
    private static partial void LogDetectedViaMcpClientInfo(ILogger logger, string? name, string? version);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Caller detected via model hint header: {Hint}")]
    private static partial void LogDetectedViaModelHintHeader(ILogger logger, string hint);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Caller detected via User-Agent: {UA}")]
    private static partial void LogDetectedViaUserAgent(ILogger logger, string ua);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Caller detected via OllamaModel config: {Model}")]
    private static partial void LogDetectedViaOllamaConfig(ILogger logger, string model);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Caller not identified; using default CallerInfo")]
    private static partial void LogCallerNotIdentified(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "CallerDetectionService threw unexpectedly; using default")]
    private static partial void LogCallerDetectionThrew(ILogger logger, Exception ex);

    private const string ModelHintHeader = "X-Model-Hint";
    private const string LlmModelHeader = "X-LLM-Model";
    private readonly string _configuredOllamaModel;
    private readonly ILogger<CallerDetectionService> _logger;

    public CallerDetectionService(
        string configuredOllamaModel,
        ILogger<CallerDetectionService> logger)
    {
        _configuredOllamaModel = configuredOllamaModel;
        _logger = logger;
    }

    public CallerInfo Detect(
        HttpContext httpContext,
        string? mcpClientName = null,
        string? mcpClientVersion = null)
    {
        try
        {
            // 1 — MCP initialize handshake (values extracted by the tool handler)
            if (!string.IsNullOrWhiteSpace(mcpClientName))
            {
                string? modelHint = TryGetHeader(httpContext, ModelHintHeader)
                                ?? TryGetHeader(httpContext, LlmModelHeader);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    LogDetectedViaMcpClientInfo(_logger, mcpClientName, mcpClientVersion);
                }

                return new CallerInfo
                {
                    ClientName = mcpClientName,
                    ClientVersion = mcpClientVersion,
                    ModelHint = Normalize(modelHint),
                    UserAgent = TryGetHeader(httpContext, "User-Agent"),
                    Source = DetectionSource.McpClientInfo
                };
            }

            // 2 & 3 — Explicit model hint header
            string? explicitHint = TryGetHeader(httpContext, ModelHintHeader)
                               ?? TryGetHeader(httpContext, LlmModelHeader);
            if (!string.IsNullOrWhiteSpace(explicitHint))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    LogDetectedViaModelHintHeader(_logger, explicitHint);
                }
                return new CallerInfo
                {
                    ModelHint = Normalize(explicitHint),
                    UserAgent = TryGetHeader(httpContext, "User-Agent"),
                    Source = DetectionSource.ModelHintHeader
                };
            }

            // 4 — User-Agent
            string? ua = TryGetHeader(httpContext, "User-Agent");
            if (!string.IsNullOrWhiteSpace(ua))
            {
                LogDetectedViaUserAgent(_logger, ua);
                return new CallerInfo
                {
                    ModelHint = ExtractModelFromUserAgent(ua),
                    UserAgent = ua,
                    Source = DetectionSource.UserAgent
                };
            }

            // 5 — TokenSaverConfig.OllamaModel
            if (!string.IsNullOrWhiteSpace(_configuredOllamaModel))
            {
                LogDetectedViaOllamaConfig(_logger, _configuredOllamaModel);
                return new CallerInfo
                {
                    ModelHint = Normalize(_configuredOllamaModel),
                    Source = DetectionSource.OllamaConfig
                };
            }

            // 6 — Default
            LogCallerNotIdentified(_logger);
            return new CallerInfo { Source = DetectionSource.Default };
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or ArgumentException)
        {
            // Scoped to the failure modes header/string parsing above can plausibly produce
            // (e.g. a malformed header value tripping something downstream, or an unexpected
            // ASP.NET Core header-collection state) rather than a blanket catch(Exception).
            // Every call site already null-checks HttpContext before calling Detect(), so a
            // NullReferenceException here would indicate a real bug, not an expected runtime
            // condition — it's allowed to propagate instead of being silently downgraded to a
            // default CallerInfo.
            LogCallerDetectionThrew(_logger, ex);
            return new CallerInfo { Source = DetectionSource.Default };
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? TryGetHeader(HttpContext ctx, string name)
    {
        return ctx.Request.Headers.TryGetValue(name, out StringValues v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : null;
    }

    private static string Normalize(string? value)
    {
        return (value ?? "").Trim().ToUpperInvariant();
    }

    private static string? ExtractModelFromUserAgent(string ua)
    {
        string lower = ua.ToUpperInvariant();
        string[] knownTokens =
        [
            "claude", "sonnet", "opus", "haiku",
            "qwen",
            "deepseek",
            "llama", "mistral", "mixtral", "gemma"
        ];
        foreach (string token in knownTokens)
        {
            if (lower.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return token;
            }
        }

        return lower;
    }
}
