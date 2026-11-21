# Source reference

Reference for the public types in `src/`. The package bundles five projects:

| Project | Role |
| --- | --- |
| `AiDevHelpers.AgentHelpers.Core` | Contracts, caller detection, adapter registry, context budgeting |
| `AiDevHelpers.AgentHelpers.Anthropic` | Claude Sonnet, Opus, Haiku |
| `AiDevHelpers.AgentHelpers.DeepSeek` | DeepSeek R1, V3, Coder |
| `AiDevHelpers.AgentHelpers.Qwen` | Qwen 2.5, Qwen 3.x, QwQ |
| `AiDevHelpers.AgentHelpers.OpenSource` | Llama, Mistral/Mixtral, Gemma, and client adapters for Continue, Copilot, and Cursor |

## Flow

1. `ICallerDetectionService.Detect` builds a `CallerInfo` from the request.
2. `AgentAdapterRegistry.ResolveBoth` picks one model adapter and one client adapter.
3. `IAgentAdapter.GetFormat` returns a `ContextFormat`. For client adapters, `ContextFormat.WithClientOverrides` merges it with the model format.
4. `IAgentAdapter.AdaptContext` formats the raw context. Model adapters delegate to a `*ContextFormatter`, and client adapters add a header and trim to the token budget.

## Core

### Contracts

- **`AdapterKind`** (enum): `Model` detects the AI model family. `Client` detects the calling app.
- **`IAgentAdapter`**: `Family`, `Priority`, `Kind`, `CanHandle(CallerInfo)`, `GetFormat(CallerInfo)`, `AdaptContext(rawContext, caller, format)`.
- **`ICallerDetectionService`**: `Detect(HttpContext, mcpClientName?, mcpClientVersion?)`. Never throws.

### Models

- **`CallerInfo`** (record): `ClientName`, `ClientVersion`, `ModelHint`, `UserAgent`, `Source`.
- **`DetectionSource`** (enum): `McpClientInfo`, `ModelHintHeader`, `UserAgent`, `OllamaConfig`, `Default`.
- **`ContextFormat`** (record): formatting flags, `ContextTokenBudget`, and client flags `WillAskForMissingContext` and `PerFileTokenLimit`. Predefined instances:

| Instance | Budget | Notes |
| --- | --- | --- |
| `AnthropicSonnet` | 80,000 | XML tags, prompt-cache ordering |
| `AnthropicOpus` | 120,000 | As Sonnet, larger budget |
| `AnthropicHaiku` | 40,000 | Short focused prompts |
| `Qwen` | 100,000 | Output schema and self-check |
| `DeepSeekR1` | 60,000 | No system prompt, no CoT |
| `DeepSeekV3` | 60,000 | XML and Markdown |
| `Llama` | 32,000 | Markdown headers |
| `Mistral` | 32,000 | Short focused prompts |
| `Gemma` | 32,000 | No system prompt |
| `Copilot` | 25,000 | 8,000 per-file limit, does not ask for missing context |
| `Continue` | 40,000 | 10,000 per-file limit, does not ask for missing context |
| `Cursor` | 60,000 | Asks for missing context |
| `Default` | 60,000 | Used when nothing matches |

`WithClientOverrides` takes `WillAskForMissingContext` and `PerFileTokenLimit` from the client format. For `PerFileTokenLimit`, 0 means no limit, and when both sides set a limit the lower one wins.

### Services

- **`AgentAdapterRegistry`**: adapters are sorted by `Priority`, highest first.
  - `Resolve(caller)` returns the first adapter whose `CanHandle` is true, or `DefaultAdapter`.
  - `ResolveBoth(caller)` returns a `(model, client)` tuple. Either side falls back to `DefaultAdapter`.
- **`CallerDetectionService`**: detection waterfall, first match wins.
  1. MCP `clientName` (with `X-Model-Hint` or `X-LLM-Model` as the model hint)
  2. `X-Model-Hint` or `X-LLM-Model` header
  3. `User-Agent` header, matched against known model tokens
  4. The configured Ollama model name
  5. Empty default
- **`ContextPartParser.Split(raw)`**: splits raw context on `\n\n` into `Summary`, `RequestedFiles`, `History`, and `Prompt`. The prompt is the last part. Returns a `ParsedContextParts` record.
- **`TokenEstimator`**: `Estimate` (chars / 4), `FitsInBudget`, and `TrimToBudget`. `TrimToBudget` cuts at the last newline under the limit and appends `[...context trimmed to fit token budget...]`.
- **`DefaultAgentAdapter`** (internal): priority `int.MinValue`, always matches, returns `ContextFormat.Default` and trims to budget.

### DI

- **`AddAgentHelpers(services, ollamaModelName = "")`** registers `ICallerDetectionService` and `AgentAdapterRegistry`.
- **`AddAdapter<TAdapter>()`** registers one `IAgentAdapter`. Chain it for each adapter.

```csharp
builder.Services
    .AddAgentHelpers(config.OllamaModel)
    .AddAdapter<AnthropicAgentAdapter>()
    .AddAdapter<QwenAgentAdapter>()
    .AddAdapter<DeepSeekAgentAdapter>()
    .AddAdapter<OpenSourceAgentAdapter>();
```

The client adapters in `OpenSource` (`ContinueClientAdapter`, `CopilotClientAdapter`, `CursorClientAdapter`) are not in the example above, so register them too if you want client-specific limits.

## Model adapters

Each model adapter has a priority, a `CanHandle` test on `ClientName` or `ModelHint`, a variant enum, and a formatter.

| Adapter | Priority | Matches | Variants | Formatter |
| --- | --- | --- | --- | --- |
| `AnthropicAgentAdapter` | 100 | Client `claude-code`, `claude-ai`, `claude`, `claude-desktop`, `cowork`; or hint contains `claude`, `sonnet`, `opus`, `haiku` | `Sonnet`, `Opus`, `Haiku` | `AnthropicContextFormatter` |
| `DeepSeekAgentAdapter` | 80 | Hint contains `deepseek` | `R1`, `V3`, `Coder` | `DeepSeekContextFormatter` |
| `QwenAgentAdapter` | 80 | Hint contains `qwen` or `qwq` | `Qwen2`, `Qwen3`, `Qwen3Thinking` | `QwenContextFormatter` |
| `OpenSourceAgentAdapter` | 60 | Hint contains `llama`, `mistral`, `mixtral`, `gemma` | `Llama`, `Mistral`, `Gemma` | `OpenSourceContextFormatter` |

Variant rules:

- **Anthropic:** `haiku` wins over `opus`, and anything else is `Sonnet`.
- **DeepSeek:** `r1` or `reasoner` is `R1`, `coder` is `Coder`, anything else is `V3`.
- **Qwen:** `qwq`, `thinking`, `plus`, or `qwen3.5` is `Qwen3Thinking`. `qwen3` is `Qwen3`. Anything else is `Qwen2`. `Qwen3Thinking` uses a 500,000-token budget.
- **OpenSource:** `gemma` is `Gemma`, `mistral` or `mixtral` is `Mistral`, anything else is `Llama`.

Each formatter exposes `public static string Format(string rawContext, <Variant> variant, ContextFormat format)`.

## Client adapters (OpenSource)

Each client adapter is `AdapterKind.Client` with priority 90. It matches on `ClientName` (case-insensitive), returns a fixed format, and prepends a short header before trimming to the budget.

| Adapter | Matches | Format | Header note |
| --- | --- | --- | --- |
| `ContinueClientAdapter` | `continue`, `continue.dev`, `continuedev` | `Continue` (10 K per file) | Says content was split or truncated and that Continue won't fetch more |
| `CopilotClientAdapter` | `copilot`, `github-copilot`, `vscode-copilot`, `github.copilot`, `github copilot`, `copilot-chat` | `Copilot` (8 K per file) | Says summaries are labelled and Copilot won't fetch more |
| `CursorClientAdapter` | `cursor`, `cursor-ide` | `Cursor` (asks for missing context) | Says Cursor can re-read listed files |

## Known issues

- **`ModelHint` is documented as lowercase, but it is stored uppercase.** `CallerDetectionService.Normalize` and `ExtractModelFromUserAgent` return uppercase values, and the `CallerInfo.ModelHint` doc says "Lower-cased before storage." Adapter matching is case-insensitive, so detection works. The doc is wrong.
- **Variant detection for Anthropic, DeepSeek, and OpenSource** was case-sensitive and never matched. It now compares case-insensitively. Tests are in `tests/AiDevHelpers.AgentHelpers.Core.Tests/VariantDetectionTests.cs`.
