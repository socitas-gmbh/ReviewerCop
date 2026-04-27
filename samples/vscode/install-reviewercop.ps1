<#
.SYNOPSIS
    Install or update Socitas.ReviewerCop analyzers for the AL Language extension in VS Code.

.DESCRIPTION
    Downloads Socitas.ReviewerCop.dll and Socitas.AICop.dll from the latest
    matching GitHub Release of socitas/ReviewerCop and copies them into the
    Microsoft AL Language extension's bin/Analyzers folder so they can be
    referenced from al.codeAnalyzers via the ${analyzerFolder} placeholder.

    Designed to run either as a workspace task (script lives at
    <workspace>/.vscode/install-reviewercop.ps1) or as a global user task
    (script lives at ~/.socitas/install-reviewercop.ps1).

    No NuGet client, no nuget.config, no PAT required for public releases.
    Pass -GitHubToken if you hit anonymous rate limits or the repo is private.

    Re-run after AL Language extension upgrades (the extension folder is
    versioned, so each upgrade ships a fresh empty Analyzers folder).

.PARAMETER PreRelease
    Pick the latest release including prereleases (instead of /releases/latest,
    which always returns the latest *stable* release).

.PARAMETER Version
    Pin to a specific tag, e.g. "1.2.3" or "v1.2.3". Empty = latest.

.PARAMETER Repo
    Source repo in owner/name form. Default: socitas/ReviewerCop.

.PARAMETER GitHubToken
    Optional GitHub token. Adds an Authorization header to API and asset
    requests. Useful for private repos or to avoid the 60/hr anonymous rate
    limit. Falls back to $env:GITHUB_TOKEN if unset.

.EXAMPLE
    pwsh -File .vscode/install-reviewercop.ps1
    pwsh -File .vscode/install-reviewercop.ps1 -PreRelease
    pwsh -File .vscode/install-reviewercop.ps1 -Version 1.2.3
#>
param(
    [switch]$PreRelease,
    [string]$Version = '',
    [string]$Repo    = 'socitas-gmbh/ReviewerCop',
    [string]$GitHubToken = ''
)

$ErrorActionPreference = 'Stop'

if (-not $GitHubToken -and $env:GITHUB_TOKEN) {
    $GitHubToken = $env:GITHUB_TOKEN
}

$apiBase = "https://api.github.com/repos/$Repo"
$apiHeaders = @{
    'Accept'               = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
    'User-Agent'           = 'install-reviewercop.ps1'
}
$assetHeaders = @{
    'Accept'     = 'application/octet-stream'
    'User-Agent' = 'install-reviewercop.ps1'
}
if ($GitHubToken) {
    $apiHeaders['Authorization']   = "Bearer $GitHubToken"
    $assetHeaders['Authorization'] = "Bearer $GitHubToken"
}

function Get-TargetRelease {
    if ($Version) {
        $tag = if ($Version.StartsWith('v')) { $Version } else { "v$Version" }
        Write-Host "Fetching release for tag $tag ..."
        return Invoke-RestMethod -Uri "$apiBase/releases/tags/$tag" -Headers $apiHeaders
    }
    if ($PreRelease) {
        Write-Host "Fetching latest release (including prereleases) ..."
        $all = Invoke-RestMethod -Uri "$apiBase/releases?per_page=20" -Headers $apiHeaders
        $picked = $all | Sort-Object { [datetime]$_.published_at } -Descending | Select-Object -First 1
        if (-not $picked) { throw "No releases found at $Repo." }
        return $picked
    }
    Write-Host "Fetching latest stable release ..."
    return Invoke-RestMethod -Uri "$apiBase/releases/latest" -Headers $apiHeaders
}

$release = Get-TargetRelease
Write-Host "Selected release : $($release.tag_name) (prerelease=$($release.prerelease))"

$wantedAssets = @('Socitas.ReviewerCop.dll', 'Socitas.AICop.dll')
$missing = $wantedAssets | Where-Object {
    $name = $_
    -not ($release.assets | Where-Object { $_.name -eq $name })
}
if ($missing) {
    throw "Release $($release.tag_name) is missing required asset(s): $($missing -join ', ')."
}

# Resolve the latest installed Microsoft AL Language extension.
$vscodeExtRoots = @(
    "$HOME/.vscode/extensions",
    "$HOME/.vscode-server/extensions",
    "$HOME/.vscode-insiders/extensions"
) | Where-Object { Test-Path $_ }

if (-not $vscodeExtRoots) {
    throw "No VS Code extensions folder found under your home directory."
}

$alExt = $vscodeExtRoots |
    ForEach-Object { Get-ChildItem -Path $_ -Directory -Filter 'ms-dynamics-smb.al-*' -ErrorAction SilentlyContinue } |
    Sort-Object Name -Descending |
    Select-Object -First 1

if (-not $alExt) {
    throw "Microsoft AL Language extension (ms-dynamics-smb.al-*) not found. Install it from the VS Code Marketplace and try again."
}

$analyzerDir = Join-Path $alExt.FullName 'bin/Analyzers'
New-Item -ItemType Directory -Force -Path $analyzerDir | Out-Null

foreach ($name in $wantedAssets) {
    $asset = $release.assets | Where-Object { $_.name -eq $name } | Select-Object -First 1
    $dest  = Join-Path $analyzerDir $name
    Write-Host "Downloading $name ($([math]::Round($asset.size / 1KB)) KB) ..."
    Invoke-WebRequest -Uri $asset.url -Headers $assetHeaders -OutFile $dest -UseBasicParsing
}

Write-Host ""
Write-Host "Installed analyzers ($($release.tag_name)) to:"
Write-Host "  $analyzerDir"
Write-Host ""
Write-Host "Add the following to your al.codeAnalyzers (settings.json):"
Write-Host '  "${analyzerFolder}Socitas.ReviewerCop.dll"'
Write-Host '  // optional: "${analyzerFolder}Socitas.AICop.dll"'
Write-Host ""
Write-Host "Reload the window (Ctrl+Shift+P -> 'Developer: Reload Window') to pick up the new analyzers."
