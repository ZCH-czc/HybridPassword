$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $root 'blazor\blazorApp\blazorApp\blazorApp.csproj'
$packageJsonPath = Join-Path $root 'package.json'
$issPath = Join-Path $PSScriptRoot 'windows-installer.iss'
$isccCandidates = @(
  'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
  'C:\Program Files\Inno Setup 6\ISCC.exe'
)

if (-not (Test-Path $projectPath)) {
  throw "MAUI host project was not found: $projectPath"
}

if (-not (Test-Path $issPath)) {
  throw "Inno Setup script was not found: $issPath"
}

$packageVersion = $null
if (Test-Path $packageJsonPath) {
  $packageJson = Get-Content -Path $packageJsonPath -Raw | ConvertFrom-Json
  if ($packageJson.version) {
    $packageVersion = [string]$packageJson.version
  }
}

$isccPath = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $isccPath) {
  throw 'Inno Setup 6 was not found. Install Inno Setup first, then run this script again.'
}

[xml]$projectXml = Get-Content -Path $projectPath
$projectVersion = $projectXml.Project.PropertyGroup |
  Where-Object { $_.ApplicationDisplayVersion } |
  Select-Object -First 1 -ExpandProperty ApplicationDisplayVersion
$appVersion = $null

if (-not $appVersion) {
  $appVersion = $packageVersion
}

if (-not $appVersion) {
  $appVersion = $projectVersion
}

if (-not $appVersion) {
  $appVersion = '1.0.0'
}

Write-Host "Installer version: $appVersion"
Write-Host "Publishing Windows host..."
& dotnet publish $projectPath -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None
if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host "Building installer..."
& $isccPath "/DAppVersion=$appVersion" $issPath
if ($LASTEXITCODE -ne 0) {
  throw "ISCC failed with exit code $LASTEXITCODE"
}

$installerDir = Join-Path $root 'blazor\blazorApp\blazorApp\bin\Release\Installer'
if (Test-Path $installerDir) {
  Write-Host "Installer output: $installerDir"
}
