# Move from TokenSaver

The AgentHelpers libraries moved out of `rsperry79/TokenSaver` (`src/AgentHelpers`) into this repo.
They ship as one NuGet package, `AiDevHelpers.AgentHelpers`.

## What changed

- **Source:** the five projects moved unchanged at first. Namespaces were `TokenSaver.AgentHelpers.*` and
  were later renamed to `AiDevHelpers.AgentHelpers.*`. Consumers must update their `using` directives.
- **History:** this repo starts fresh. The TokenSaver commit history was not carried over.
- **Packaging:** `src/AiDevHelpers.AgentHelpers` is the only packable project. It references the five
  helper projects with `PrivateAssets="all"` and copies each helper DLL into `lib/net10.0/` through the
  `PackHelperAssemblies` target in `AiDevHelpers.AgentHelpers.csproj`. `SuppressDependenciesWhenPacking`
  keeps the nuspec free of project dependencies. The package also contains an empty
  `AiDevHelpers.AgentHelpers.dll`, which is harmless.
- **Versioning:** Nerdbank.GitVersioning reads `version.json`. Pushes to `main` publish.

## Consumers

TokenSaver's `src/Host/TokenSaver.MCP/TokenSaver.MCP.csproj` references
`AiDevHelpers.AgentHelpers` 1.0.1-alpha instead of the five `ProjectReference`s. CI adds the
`https://nuget.pkg.github.com/AiDevHelpers/index.json` source with `TOKENSAVER_READ_TOKEN`, which
needs `read:packages`.

Local development needs the same source on each machine, with a PAT that has `read:packages`.

## Publishing

`.github/workflows/ci.yml` builds and packs on every push and PR. On pushes to `main` only, it pushes
the `.nupkg` to GitHub Packages with `GITHUB_TOKEN`. To release, update `version.json` and merge to
`main`.
