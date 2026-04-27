# Install ReviewerCop in VS Code

This folder contains a small PowerShell script and a couple of `tasks.json` / `settings.json` snippets that wire ReviewerCop into the Microsoft AL Language extension.

The script downloads `Socitas.ReviewerCop.dll` and `Socitas.AICop.dll` from the **GitHub Releases** of [socitas/ReviewerCop](https://github.com/socitas/ReviewerCop) and copies them into the AL Language extension's `bin/Analyzers/` folder. From there, `${analyzerFolder}` in `al.codeAnalyzers` resolves them.

> **No NuGet, no PAT, no nuget.config required for this path.** Public releases are downloaded anonymously. Pass `-GitHubToken` only if you hit anonymous rate limits or fork to a private repo. (Pipelines still use the `nuget install` flow via GitHub Packages — see [`../azure-pipelines/README.md`](../azure-pipelines/README.md).)

Two install styles:

- **[Workspace task](#option-a--workspace-task)** — script and tasks live in `.vscode/` of each AL workspace. Commit-friendly: every team member gets the task on `git clone`.
- **[Global user task](#option-b--global-user-task)** — script lives in `~/.socitas/`, the task is defined once in your VS Code user tasks. Available in every workspace with no per-repo setup.

## Prerequisites (both options)

- VS Code with the [AL Language extension](https://marketplace.visualstudio.com/items?itemName=ms-dynamics-smb.al) installed.
- [PowerShell 7+](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell) (`pwsh`) on your `PATH`.
- Internet access to `api.github.com` (the script makes one API call to find the latest release, then downloads the two DLL assets).

That's it — no other tooling, no auth.

---

## Option A — Workspace task

Files live inside the AL workspace. Best when you want the install task checked into the repo so every team member gets it on `git clone`.

### 1. Drop the files into your AL workspace

Copy these three files from this folder into the matching paths in your AL workspace:

| Source                                     | Destination in your workspace                |
|--------------------------------------------|----------------------------------------------|
| `samples/vscode/install-reviewercop.ps1`   | `.vscode/install-reviewercop.ps1`            |
| `samples/vscode/tasks.json` *(merge)*      | `.vscode/tasks.json`                         |
| `samples/vscode/settings.json` *(merge)*   | `.vscode/settings.json`                      |

`tasks.json` and `settings.json` are snippets — merge their entries into existing files rather than overwriting them.

### 2. Run the install task

From the VS Code command palette: **Tasks: Run Task → ReviewerCop: Install / Update**.

The task downloads the latest stable release's DLLs and copies them into the AL Language extension's `bin/Analyzers/`. To use prereleases, run **ReviewerCop: Install / Update (prerelease)** instead.

### 3. Reload the window

**Developer: Reload Window** from the command palette. The AL Language extension picks up the new analyzers and starts emitting diagnostics in your AL files.

---

## Option B — Global user task

Script lives in `~/.socitas/`. The task is defined once in VS Code's user-level tasks file and is available in every workspace — no per-repo setup, nothing to commit.

### 1. Copy the script to `~/.socitas/`

```powershell
# Linux / macOS / Windows (PowerShell)
$dest = Join-Path $HOME '.socitas'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item samples/vscode/install-reviewercop.ps1 $dest/
```

### 2. Add the task to your user-level tasks.json

Open the VS Code command palette → **Tasks: Open User Tasks** (pick "Other" if prompted for a template). Merge the entries from [`samples/vscode/global-tasks.json`](global-tasks.json) into the resulting file.

The task references the script via the `${userHome}` VS Code variable, so it works on any platform with no path edits.

### 3. Add the analyzer entries to your settings

Merge the [`settings.json`](settings.json) snippet into your **user** settings (Command Palette → **Preferences: Open User Settings (JSON)**) so it applies in every workspace, or into per-workspace `.vscode/settings.json` if you'd rather opt in workspace-by-workspace. The `${analyzerFolder}` placeholder resolves regardless of which workspace is open.

### 4. Run the task from any workspace

Open any AL workspace, then **Tasks: Run Task → ReviewerCop: Install / Update**. Reload the window. Done.

After an AL Language extension upgrade, run the task again — see [After an AL Language extension upgrade](#after-an-al-language-extension-upgrade) below.

---

## Enabling AICop (opt-in)

`Socitas.AICop.dll` is downloaded alongside `Socitas.ReviewerCop.dll` but not enabled by default. To turn it on, uncomment the AICop line in your `al.codeAnalyzers` array:

```jsonc
"al.codeAnalyzers": [
  "${CodeCop}",
  "${UICop}",
  "${analyzerFolder}Socitas.ReviewerCop.dll",
  "${analyzerFolder}Socitas.AICop.dll"
]
```

Reload the window after the change.

## Pinning a specific version

By default the task pulls the latest *stable* release. To pin:

```powershell
pwsh -File <script> -Version 1.2.3
```

Or add a tasks.json variant that always passes `-Version`:

```jsonc
{
  "label": "ReviewerCop: Install pinned 1.2.3",
  "type": "shell",
  "command": "pwsh",
  "args": ["-NoProfile", "-File", "${workspaceFolder}/.vscode/install-reviewercop.ps1", "-Version", "1.2.3"]
}
```

## After an AL Language extension upgrade

The AL extension installs into a versioned folder (e.g. `ms-dynamics-smb.al-15.4.123456`), so each upgrade ships a fresh empty `bin/Analyzers/`. Re-run **ReviewerCop: Install / Update** after upgrading; the script picks up the newest extension folder automatically.

## Hitting GitHub anonymous rate limits

GitHub allows 60 unauthenticated API requests per IP per hour. If multiple machines behind the same NAT (or a busy CI box) blow through that, mint a [classic PAT](https://github.com/settings/tokens) (no scopes needed for public repos) and either:

- Set `GITHUB_TOKEN` in your environment — the script reads it automatically; or
- Pass it explicitly: `-GitHubToken <token>`.

## Troubleshooting

- **404 on `/releases/latest`**: There may not yet be any tagged release. Try `-PreRelease` to include all releases.
- **"missing required asset(s)"**: The release exists but doesn't have the raw DLL assets attached. This means the release was cut before the workflow that attaches DLLs ([build-and-release.yml](../../.github/workflows/build-and-release.yml)) was active. Use a newer release or pin a version known to have the assets.
- **"Microsoft AL Language extension not found"**: Install the extension from the Marketplace, then re-run the task.
- **Diagnostics don't appear after reload**: Confirm the DLLs landed in the analyzer folder printed by the task. Check that `${analyzerFolder}` entries in `al.codeAnalyzers` use exact filenames (case matters on Linux/macOS).
