# AiDevHelpers.AgentHelpers

Helpers that adapt TokenSaver's concise context to the AI model or client that will read it. The
package is `AiDevHelpers.AgentHelpers`. It bundles five assemblies:

| Assembly | Purpose |
|---|---|
| `AiDevHelpers.AgentHelpers.Core` | Contracts, caller detection, adapter registry, context parsing, token estimates |
| `AiDevHelpers.AgentHelpers.Anthropic` | Claude Sonnet, Opus and Haiku formatting |
| `AiDevHelpers.AgentHelpers.DeepSeek` | DeepSeek R1 and V3 formatting |
| `AiDevHelpers.AgentHelpers.Qwen` | Qwen formatting |
| `AiDevHelpers.AgentHelpers.OpenSource` | Llama, Mistral, Gemma, and the Copilot, Continue and Cursor client adapters |

Namespaces match the assembly names: `AiDevHelpers.AgentHelpers.*`.

## What it does

Given a caller, the library picks two adapters and merges their formats:

1. **Model adapter:** matches the AI model family, such as Claude or Qwen. It decides prompt formatting,
   system-prompt support and chain-of-thought hints.
2. **Client adapter:** matches the calling app, such as Cursor, Copilot or Continue. It decides
   behavior flags such as `WillAskForMissingContext` and `PerFileTokenLimit`.

The result is a `ContextFormat` (budget, section style, flags). An adapter's `AdaptContext` then
reshapes the raw context for that caller.

## Architecture

```
HTTP request ──► CallerDetectionService ──► CallerInfo
                                              │
                         AgentAdapterRegistry.ResolveBoth
                          │                         │
                  model adapter              client adapter
                  (AdapterKind.Model)        (AdapterKind.Client)
                          │                         │
                          └──── ContextFormat merge ┘
                                              │
                          IAgentAdapter.AdaptContext(raw, format)
```

- **`CallerDetectionService`** builds a `CallerInfo` from the MCP client info, the `X-Model-Hint` or
  `X-LLM-Model` header, and finally the `User-Agent` string. Each `CallerInfo` records which source
  matched (`DetectionSource`).
- **`IAgentAdapter`** has a `Family`, a `Priority`, a `Kind`, and `CanHandle(CallerInfo)`. It returns a
  `ContextFormat` and can reshape raw context. Higher `Priority` wins when several adapters match.
- **`AgentAdapterRegistry`** holds all registered adapters. `Resolve` returns the best single adapter,
  falling back to `DefaultAdapter`. `ResolveBoth` returns one model adapter and one client adapter.
- **`ContextFormat`** is a record of presets, such as `AnthropicSonnet`, `DeepSeekR1` and `Cursor`.
  `ContextFormat.Default` is used when nothing matches.
- **`ContextPartParser`** splits raw context into its parts: the optional summary, the optional
  requested-files line, up to five history entries, and the final user prompt. Every formatter shares it.
- **`TokenEstimator`** estimates tokens, checks budgets, and trims text to a budget.

## Usage

Register the library in the host's DI container:

```csharp
services.AddAgentHelpers(config.OllamaModel);
```

`AddAgentHelpers` registers the caller detector, the registry and the default adapter. Each model or
client adapter is registered with `AddAdapter<TAdapter>()`.

## Build

```powershell
dotnet restore AiDevHelpers.AgentHelpers.slnx
dotnet build AiDevHelpers.AgentHelpers.slnx -c Release --no-restore
dotnet pack src/AiDevHelpers.AgentHelpers -c Release --no-build -o artifacts
```

Versions come from `version.json` through Nerdbank.GitVersioning.

## Test

```powershell
dotnet test AiDevHelpers.AgentHelpers.slnx -c Release
```

Tests live under `tests/` and use xUnit. `AiDevHelpers.AgentHelpers.Core.Tests` covers the token
estimator.

## Branches

- `main`: release branch. Protected: PRs only, the `Build and pack` check must pass, no force pushes
  or deletion. Merges need explicit approval.
- `dev`: integration branch. Release PRs merge here. Same protection as `main`.
- `wip`: work in progress. Protected against force pushes and deletion only. No PR or check rules.

## Publishing

Every push and PR builds and packs. Pushes to `main` also publish the package to
`https://nuget.pkg.github.com/AiDevHelpers/index.json`. Consumers need a PAT with `read:packages` for
that feed.

See [docs/source-reference.md](docs/source-reference.md) for the public types, adapters, and detection
rules. See [docs/move-from-tokensaver.md](docs/move-from-tokensaver.md) for the history of the move from
TokenSaver.
