# ReviewerCop

A Roslyn-based code analyzer for [AL](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-programming-in-al) (Microsoft Dynamics 365 Business Central) that enforces company-specific coding conventions and best practices.

> **Note:** Most of the code in this repository is AI-generated.

## Overview

ReviewerCop is distributed as the `ALCops.CompanyCop` NuGet package. It hooks into the AL compiler via the BC Development Tools SDK and reports diagnostics directly in your editor and CI pipeline.

This project is based on and inspired by [ALCops/Analyzers](https://github.com/ALCops/Analyzers), used under the MIT License.

## Rules

| ID | Title | Category | Severity |
|----|-------|----------|----------|
| CC0001 | No TODO Comments | Style | Warning |
| CC0002 | No Global Variables | Style | Warning |
| CC0003 | Validate Field Assignments | Usage | Warning |
| CC0004 | No Type or Prefix in Variable Name | Naming | Warning |
| CC0005 | Event Subscriber Naming Convention | Naming | Warning |
| CC0006 | Use SetLoadFields | Performance | Info |
| CC0008 | No Modify in OnValidate | Style | Warning |
| CC0009 | Data Classification on Table | Design | Warning |
| CC0010 | Label Comment for Placeholders | Style | Info |

## Building

**Prerequisites:** .NET 8 SDK and BC Development Tools for supported Business Central versions 16 and later (obtained automatically in CI via the provided GitHub Actions).

```bash
dotnet build
```

To build the NuGet package:

```bash
dotnet pack ./src/ALCops.CompanyCop/ALCops.CompanyCop.csproj --configuration Release /p:ContinuousIntegrationBuild=true
```

## Testing

```bash
dotnet test
```

## CI/CD

GitHub Actions workflows handle build, test, and release:

- **Pull requests** — build and test
- **Push to `main`** — build, test, and publish a pre-release package to GitHub Packages
- **Tag `v*`** — create a GitHub Release and publish to both GitHub Packages and NuGet.org

Versioning is managed via [GitVersion](https://gitversion.net/) using GitHub Flow.

## License

See [LICENSE](LICENSE).
