# ReviewerCop

A Roslyn-based code analyzer for [AL](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-programming-in-al) (Microsoft Dynamics 365 Business Central) that enforces company-specific coding conventions and best practices.

> **Note:** Most of the code in this repository is AI-generated.

## Overview

ReviewerCop is distributed two ways:

- **GitHub Releases** — `Socitas.ReviewerCop.dll` and `Socitas.AICop.dll` are attached as raw assets to every `v*` tag for anonymous download (used by the local VS Code task).
- **GitHub Packages NuGet feed** (`Socitas.ReviewerCop`) — used by Azure Pipelines and any `nuget install` flow.

It hooks into the AL compiler via the BC Development Tools SDK and reports diagnostics directly in your editor and CI pipeline.

The package ships **two analyzer DLLs** — `Socitas.ReviewerCop.dll` (general AL rules, `RC*` IDs) and `Socitas.AICop.dll` (AI-related rules, `AI*` IDs). ReviewerCop is enabled by default; AICop is opt-in.

This project is based on and inspired by [ALCops/Analyzers](https://github.com/ALCops/Analyzers), used under the MIT License.

## Install

- **VS Code (local development)** — see [samples/vscode/README.md](samples/vscode/README.md) for a workspace or user-level task that downloads the analyzer DLLs from GitHub Releases and wires them into `al.codeAnalyzers` via the `${analyzerFolder}` placeholder. No NuGet client, no PAT.
- **Azure Pipelines (CI)** — see [samples/azure-pipelines/README.md](samples/azure-pipelines/README.md) for the reusable `steps` template. Mirrors the existing `BusinessCentral.LinterCop` install pattern: `nuget install Socitas.ReviewerCop` against the GitHub Packages feed, then glob the DLL into `alc.exe /analyzer:`.
- **Manual (CI-style)** — `nuget install Socitas.ReviewerCop -Source https://nuget.pkg.github.com/socitas/index.json` with a GitHub PAT (`read:packages` scope) configured in `nuget.config`. The DLLs land in `Socitas.ReviewerCop/lib/net8.0/`.
- **Manual (raw)** — download `Socitas.ReviewerCop.dll` and `Socitas.AICop.dll` directly from a [Release](https://github.com/socitas/ReviewerCop/releases) and reference them from `al.codeAnalyzers`.

If you already install `BusinessCentral.LinterCop` in your pipelines, ReviewerCop is a drop-in addition: same `nuget install` pattern, same `/analyzer:` glob shape — see the [Azure Pipelines README](samples/azure-pipelines/README.md#drop-in-next-to-businesscentrallintercop) for the diff.

## Rules

| ID | Title | Category | Severity | Analyzer |
|----|-------|----------|----------|----------|
| RC0001 | No TODO Comments | Style | Warning | Reviewer Cop |
| RC0002 | Validate Field Assignments | Usage | Warning | Reviewer Cop |
| RC0003 | No Type or Prefix in Variable Name | Naming | Warning | Reviewer Cop |
| RC0004 | Event Subscriber Naming Convention | Naming | Warning | Reviewer Cop |
| RC0005 | Use SetLoadFields | Performance | Warning | Reviewer Cop |
| RC0006 | No Modify in OnValidate | Style | Warning | Reviewer Cop |
| RC0007 | Data Classification on Table | Design | Warning | Reviewer Cop |
| RC0008 | Label Comment for Placeholders | Style | Info | Reviewer Cop |
| AI0001 | No Global Variables | Style | Warning | AI Cop |
| AI0002 | Caption and ToolTip on Page | Design | Warning | AI Cop |
| AI0003 | Open Brace on Same Line | Style | Warning | AI Cop |
| AI0004 | Use Rest Client | Design | Warning | AI Cop |
| AI0005 | Initialize Rest Client with Handler | Design | Warning | AI Cop |
| AI0006 | No Exit with Default Value | Style | Warning | AI Cop |
| AI0007 | Use Action Ref | Design | Warning | AI Cop |
| AI0008 | Extension Object Member Missing SOC Suffix | Naming | Warning | AI Cop |
| AI0009 | Local Procedure Has SOC Suffix | Naming | Warning | AI Cop |

## Compatibility

The analyzer and test projects target **.NET 8**. BC Development Tools sources whose target framework is higher than `net8.0` (e.g. the `net10.0` BCArtifact published for AL `18.0.35.37418` and later previews) are excluded from the CI test matrix because their `Microsoft.Dynamics.Nav.CodeAnalysis.dll` cannot be loaded by a `net8.0` test host. Support for those versions is pending a move of the analyzer/test projects to `net10.0`.

## Building

**Prerequisites:** .NET 8 SDK and BC Development Tools for supported Business Central versions 16 and later (obtained automatically in CI via the provided GitHub Actions).

```bash
dotnet build
```

To build the NuGet package:

```bash
dotnet pack ./src/Socitas.ReviewerCop/Socitas.ReviewerCop.csproj --configuration Release /p:ContinuousIntegrationBuild=true
```

## Testing

```bash
dotnet test
```

## CI/CD

GitHub Actions workflows handle build, test, and release:

- **Pull requests** — build and test
- **Push to `main`** — build, test, and publish a prerelease package to GitHub Packages
- **Tag `v*`** — publish to GitHub Packages and create a GitHub Release with both the `.nupkg` and the raw analyzer DLLs (`Socitas.ReviewerCop.dll`, `Socitas.AICop.dll`) attached as assets

Versioning is managed via [GitVersion](https://gitversion.net/) using GitHub Flow.

## License

See [LICENSE](LICENSE).
