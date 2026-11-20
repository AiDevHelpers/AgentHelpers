# AiDevHelpers.AgentHelpers

Agent helper libraries packaged as one NuGet package, `AiDevHelpers.AgentHelpers`.
The package bundles every helper assembly:

- `TokenSaver.AgentHelpers.Core`
- `TokenSaver.AgentHelpers.Anthropic`
- `TokenSaver.AgentHelpers.DeepSeek`
- `TokenSaver.AgentHelpers.OpenSource`
- `TokenSaver.AgentHelpers.Qwen`

Namespaces are unchanged from the original TokenSaver helpers.

## Build and pack

```powershell
dotnet restore AiDevHelpers.AgentHelpers.slnx
dotnet build AiDevHelpers.AgentHelpers.slnx -c Release --no-restore
dotnet pack src/AiDevHelpers.AgentHelpers -c Release --no-build -o artifacts
```

Versions come from `version.json` through Nerdbank.GitVersioning.

## Publishing

Pushes to `main` publish the package to the GitHub Packages NuGet feed at
`https://nuget.pkg.github.com/AiDevHelpers/index.json`. Pull requests only build and pack.
