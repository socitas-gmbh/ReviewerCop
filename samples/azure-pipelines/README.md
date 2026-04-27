# Install ReviewerCop in Azure Pipelines

This folder contains a reusable steps template that installs ReviewerCop on a build agent and a complete example pipeline that consumes it.

If you already install `BusinessCentral.LinterCop` in your AL pipelines, this is a 5-minute change: add the template reference and append one or two `/analyzer:` arguments to your existing `alc.exe` invocation.

## Files

| File                        | Purpose                                                                                  |
|-----------------------------|------------------------------------------------------------------------------------------|
| `reviewercop-install.yml`   | Reusable `steps` template. Restores the package and exposes analyzer paths as variables. |
| `example-consumer.yml`      | Complete sample pipeline showing the integration end-to-end.                             |

## Prerequisites

- Azure DevOps pipeline running on a Windows agent (the AL compiler is Windows-only).
- A `nuget.config` in your repo that declares the GitHub Packages feed:
  ```xml
  <packageSources>
    <add key="socitas-github" value="https://nuget.pkg.github.com/socitas/index.json" />
  </packageSources>
  ```
- Authentication for that feed on the agent (see below).

## Authenticating against GitHub Packages

GitHub Packages requires a PAT with `read:packages` scope even for public packages. Two recommended setups:

### Option A - Service connection (preferred)

1. In Azure DevOps **Project Settings → Service connections**, create a new **NuGet** service connection with:
   - Feed URL: `https://nuget.pkg.github.com/socitas/index.json`
   - Authentication: **Basic**
   - Username: any GitHub username with access
   - Password: a GitHub PAT with `read:packages`
2. Reference the service connection from the `nuget.config` (no credentials in the file) and add `NuGetAuthenticate@1` before the install template — that task injects the credentials at runtime.

This keeps the PAT out of source control and pipeline YAML.

### Option B - Variable group (fallback)

Store `GITHUB_USERNAME` and `GITHUB_PAT` in a secret variable group and template the values into `nuget.config` at runtime. Less secure (the PAT lives in environment variables on the agent during the run); only use this where service connections aren't an option.

## Adopting the template

Add the `socitas/ReviewerCop` repo to your pipeline's `resources.repositories`, pin to a tag, and reference the template once:

```yaml
resources:
  repositories:
    - repository: reviewercop
      type: github
      endpoint: socitas-github
      name: socitas/ReviewerCop
      ref: refs/tags/v1.0.0

steps:
  - task: NuGetAuthenticate@1
  - template: samples/azure-pipelines/reviewercop-install.yml@reviewercop
    parameters:
      appSymbolFolder: $(Build.SourcesDirectory)/.alpackages
      nugetConfigPath: $(Build.SourcesDirectory)/.nuget/nuget.config
      enableAICop:     false
```

Then append the analyzer path(s) to your existing `alc.exe` call:

```powershell
$alcParameters += @("/analyzer:$(reviewercop.analyzerPath)")
# Only when enableAICop is true:
$alcParameters += @("/analyzer:$(reviewercop.aicopPath)")
```

That's it. The shape is identical to how you wire `BusinessCentral.LinterCop` today — see `example-consumer.yml` for a full pipeline.

## Template parameters

| Parameter           | Type      | Default                                       | Description                                                              |
|---------------------|-----------|-----------------------------------------------|--------------------------------------------------------------------------|
| `appSymbolFolder`   | string    | `$(Build.SourcesDirectory)/.alpackages`       | Where `nuget install` extracts the package. Same folder as your symbols. |
| `nugetConfigPath`   | string    | `$(Build.SourcesDirectory)/.nuget/nuget.config` | Path to the agent's `nuget.config`.                                    |
| `includePrerelease` | boolean   | `false`                                       | Pass `-PreRelease` to `nuget install`.                                   |
| `version`           | string    | `''` (latest)                                 | Pin a specific version, e.g. `1.2.3`.                                    |
| `enableAICop`       | boolean   | `false`                                       | Also expose `Socitas.AICop.dll` as `$(reviewercop.aicopPath)`.           |

## Output variables

| Variable                       | When set                       | Value                            |
|--------------------------------|--------------------------------|----------------------------------|
| `$(reviewercop.analyzerPath)`  | Always (after the template)    | Full path to `Socitas.ReviewerCop.dll`. |
| `$(reviewercop.aicopPath)`     | Only when `enableAICop: true`  | Full path to `Socitas.AICop.dll`.       |

## Drop-in next to BusinessCentral.LinterCop

If your pipeline currently has:

```powershell
$alcParameters += @(
    "/analyzer:$((Get-ChildItem -Path $appSymbolFolder -Recurse -Filter 'BusinessCentral.LinterCop*.dll').FullName)"
)
```

…add the template reference and one more line:

```powershell
$alcParameters += @("/analyzer:$(reviewercop.analyzerPath)")
```

Both linters will run in the same `alc.exe` invocation.
