# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

ReviewerCop is a Roslyn-based code analyzer for AL (Microsoft Dynamics 365 Business Central). It hooks into the AL compiler via the BC Development Tools SDK and reports diagnostics in the editor and CI pipeline. Distributed as the `Socitas.ReviewerCop` NuGet package.

Based on [ALCops/Analyzers](https://github.com/ALCops/Analyzers).

## Build & Test Commands

```bash
dotnet build                  # Build all projects
dotnet test                   # Run all tests
dotnet test --filter "FullyQualifiedName~NoTodoComments"  # Run tests for a single rule
dotnet pack ./src/Socitas.ReviewerCop/Socitas.ReviewerCop.csproj --configuration Release  # Build NuGet package
```

**Prerequisite:** .NET 8 SDK. The BC Development Tools DLLs must be present at `Microsoft.Dynamics.BusinessCentral.Development.Tools/` (relative to repo root). In CI, GitHub Actions fetch these automatically.

## Architecture

### Two analyzer packages

- **Socitas.ReviewerCop** — Rules for human developers. Shown as squiggles in the editor and flagged in CI. All rules must be actionable by a human reading the message.
- **Socitas.AICop** — Rules consumed exclusively by AI agents via MCP. Never active for human developers. Messages and guidance actions are written as prompts: detailed, imperative, and step-by-step.

### Three projects in `src/`

- **Socitas.ReviewerCop** — The analyzer NuGet package. Contains analyzers (in `Analyzers/`) and code fixes (in `CodeFixes/`). References `Microsoft.Dynamics.Nav.CodeAnalysis` from the BC Dev Tools SDK — this is the AL equivalent of Roslyn's `Microsoft.CodeAnalysis`.
- **Socitas.ReviewerCop.Common** — Shared utilities: reflection helpers (`Reflection/`), extension methods (`Extensions/`), constants, and settings. Merged into the main analyzer DLL via ILRepack so that `alc.exe` can load everything from one assembly.
- **Socitas.ReviewerCop.Test** — NUnit tests using `ALCops.RoslynTestKit`.

### How analyzers work

Each analyzer is a class in `Analyzers/` that extends `DiagnosticAnalyzer` (from `Microsoft.Dynamics.Nav.CodeAnalysis`, not Roslyn). It registers syntax node actions via `Initialize(AnalysisContext)`. The AL syntax tree uses `SyntaxKind` values accessed through `EnumProvider.SyntaxKind` (a reflection wrapper in Common, because the enum is internal in the BC SDK).

### Adding a new rule (ReviewerCop)

1. Add a diagnostic ID in `DiagnosticIds.cs` (format: `CC####`).
2. Add resource strings (Title, MessageFormat, Description) in `ReviewerCopAnalyzers.resx`. The `.cs` file is auto-generated from the resx.
3. Add a `DiagnosticDescriptor` in `DiagnosticDescriptors.cs`.
4. Create the analyzer class in `Analyzers/`.
5. **Always evaluate whether a code fix is possible.** If the correction is mechanical and unambiguous (rename, add keyword, remove node, reorder), implement a code fix in `CodeFixes/`. Only skip a fix when the correct change genuinely requires human judgment.
6. Add comprehensive tests in `Test/Rules/<RuleName>/` — see test requirements below.

### Adding a new rule (AICop)

Follow the same steps 1–4 using `DiagnosticIds.cs`, `AICopAnalyzers.resx`, `DiagnosticDescriptors.cs`, and `Analyzers/` in the AICop project. Then:

5. **Every AICop rule must have a guidance code action.** The guidance action title IS the fix prompt — write it as a numbered, imperative instruction an AI can follow directly.
   - If a mechanical code fix also exists: add the guidance registration inside the existing fix provider's `RegisterCodeFixesAsync`, after the mechanical fix registration. Use the shared `GuidanceCodeAction` class from `CodeFixes/GuidanceCodeAction.cs`.
   - If no mechanical fix exists: create a separate guidance-only provider that returns a no-op document change.
   - End the guidance text with: *"A mechanical code fix is available to apply this change automatically."* when a mechanical fix exists.
6. Update the `Description` resource string to end with: *"A guidance code action [and a mechanical code fix are] available."*
7. Add comprehensive tests in `Test/Rules/<RuleName>/`.

### Test requirements

Every rule **must** ship with all three test categories before it is considered complete:

- **`HasDiagnostic/`** — Cover the primary violation pattern **and** all significant variants (different object types, edge-case syntax, nested contexts). Each file should test one scenario; use the diagnostic marker (`[|...|]`) to pinpoint the exact location. Aim for at least 2–3 files so that the rule boundary is clear.
- **`NoDiagnostic/`** — Cover every case that looks similar but must NOT fire: compliant code, suppressed diagnostics, correct alternatives. Missing `NoDiagnostic` tests are the most common source of false positives — do not skip them.
- **`HasFix/`** — Required when a code fix exists. Each subdirectory holds a `current.al` / `expected.al` pair. Cover the primary fix and any variant that produces different output.

Run `dotnet test` after adding tests. A rule with no automated test coverage will not be merged.

### Test conventions

Tests live in `Test/Rules/<RuleName>/<RuleName>.cs`. Each test class extends `NavCodeAnalysisBase` and uses `RoslynFixtureFactory` to create test fixtures. AL code samples are `.al` files organized in subdirectories:

- `HasDiagnostic/` — AL files where the rule should fire (markers indicate expected diagnostic locations)
- `NoDiagnostic/` — AL files where the rule should NOT fire
- `HasFix/` — Subdirectories with `current.al` / `expected.al` pairs for code fix tests

### Key types in Common

- `EnumProvider.SyntaxKind` — Reflection-based access to internal BC SDK `SyntaxKind` enum values.
- `RoslynFixtureFactory` — Creates `AnalyzerTestFixture` / `CodeFixTestFixture` instances wired up with BC SDK compilation.

## Versioning

Managed via [GitVersion](https://gitversion.net/) using GitHub Flow. Tags `v*` trigger releases to NuGet.org.

## CI behavior differences

The csproj files have `ContinuousIntegrationBuild` conditionals: in CI, `ReviewerCopAnalyzers.cs` (auto-generated from resx) is excluded from compilation (it's regenerated during build), and the test project uses binary references instead of project references.
