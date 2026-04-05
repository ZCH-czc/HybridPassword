$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $root 'blazor\blazorApp\blazorApp\blazorApp.csproj'
$packageJsonPath = Join-Path $root 'package.json'
$androidReleaseDir = Join-Path $root 'blazor\blazorApp\blazorApp\bin\Release\Android'
$androidPublishDir = Join-Path $root 'blazor\blazorApp\blazorApp\bin\Release\net10.0-android\publish'

if (-not (Test-Path $projectPath)) {
  throw "MAUI host project was not found: $projectPath"
}

$packageVersion = $null
if (Test-Path $packageJsonPath) {
  $packageJson = Get-Content -Path $packageJsonPath -Raw | ConvertFrom-Json
  if ($packageJson.version) {
    $packageVersion = [string]$packageJson.version
  }
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

Write-Host "APK version: $appVersion"
Write-Host "Publishing Android APK..."
& dotnet publish $projectPath -f net10.0-android -c Release -p:AndroidPackageFormats=apk
if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $androidPublishDir)) {
  throw "Android publish directory was not found: $androidPublishDir"
}

$signedApk = Get-ChildItem -Path $androidPublishDir -Filter '*-Signed.apk' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$unsignedApk = Get-ChildItem -Path $androidPublishDir -Filter '*.apk' | Where-Object { $_.Name -notlike '*-Signed.apk' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1

$sourceApk = if ($signedApk) { $signedApk } else { $unsignedApk }
if (-not $sourceApk) {
  throw 'No APK file was found after publish.'
}

New-Item -Path $androidReleaseDir -ItemType Directory -Force | Out-Null
$targetFileName = "PasswordVault_$appVersion`_android.apk"
$targetPath = Join-Path $androidReleaseDir $targetFileName

Copy-Item -Path $sourceApk.FullName -Destination $targetPath -Force

Write-Host "APK output: $targetPath"
