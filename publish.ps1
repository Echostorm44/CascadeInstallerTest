# Publishes the packaged dist/ to GitHub Releases with an update manifest + delta patches.
# Run pack.ps1 first. Requires a GitHub token with repo access.
#
# Usage:  pwsh -File publish.ps1 -Token ghp_xxx              (version from csproj)
#         pwsh -File publish.ps1 -Token ghp_xxx -Version 2.0.0
param(
    [Parameter(Mandatory = $true)][string]$Token,
    [string]$Version = ""
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$appId = "B7E3C1A4-9F2D-4E60-8A11-CC0FE2D3A456"
$cascade = Join-Path $root "..\CascadeUI\src\Cascade.UI.Tools\bin\Debug\net10.0\cascade.exe"

if ([string]::IsNullOrEmpty($Version)) {
    [xml]$proj = Get-Content (Join-Path $root "CascadeInstallerTest.csproj")
    $Version = ($proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
}

& $cascade publish --update-manifest `
    --github Echostorm44/CascadeInstallerTest `
    --token $Token `
    --dist (Join-Path $root "dist") `
    --app-id $appId `
    --rid win-x64 `
    --version $Version `
    --notes "CascadeInstallerTest $Version"
