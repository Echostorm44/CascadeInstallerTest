# Packages CascadeInstallerTest into a full-package zip for the updater:
#   1. publishes the app (framework-dependent, no AOT — fast to iterate the dogfood loop)
#   2. publishes the cascade-update shim alongside it (so ApplyAndRestart can hand off)
#   3. zips the result to dist/CascadeInstallerTest-<version>-win-x64.zip
#
# Usage:  pwsh -File pack.ps1            (version read from the csproj)
#         pwsh -File pack.ps1 -Version 2.0.0
param(
    [string]$Version = ""
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$rid = "win-x64"

if ([string]::IsNullOrEmpty($Version)) {
    [xml]$proj = Get-Content (Join-Path $root "CascadeInstallerTest.csproj")
    $Version = ($proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
}
Write-Host "Packaging CascadeInstallerTest $Version ($rid)..."

$publish = Join-Path $root "publish"
$dist = Join-Path $root "dist"
$shimProj = Join-Path $root "..\CascadeUI\src\Cascade.UI.Updater.Shim\Cascade.UI.Updater.Shim.csproj"
$shimOut = Join-Path $root "shim-publish"

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
if (Test-Path $shimOut) { Remove-Item $shimOut -Recurse -Force }
New-Item -ItemType Directory -Force -Path $dist | Out-Null

# 1. App
dotnet publish (Join-Path $root "CascadeInstallerTest.csproj") -c Release -r $rid `
    --self-contained false -p:PublishAot=false -p:Version=$Version -o $publish
if ($LASTEXITCODE -ne 0) { throw "app publish failed" }

# 2. Shim, copied in alongside the app
dotnet publish $shimProj -c Release -r $rid --self-contained false -p:PublishAot=false -o $shimOut
if ($LASTEXITCODE -ne 0) { throw "shim publish failed" }
Copy-Item (Join-Path $shimOut "*") $publish -Recurse -Force

# 3. Zip
$zip = Join-Path $dist "CascadeInstallerTest-$Version-$rid.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publish, $zip)

Remove-Item $shimOut -Recurse -Force
Write-Host "✓ Wrote $zip"
