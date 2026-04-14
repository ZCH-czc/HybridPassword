$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$companionProjectDir = Join-Path $repoRoot "windows-passkey-plugin\PasswordVault.PasskeyCompanion"
$companionProject = Join-Path $companionProjectDir "PasswordVault.PasskeyCompanion.csproj"
$manifestTemplate = Join-Path $companionProjectDir "appxmanifest.xml"
$configuration = "Debug"
$framework = "net10.0-windows10.0.19041.0"
$outputDir = Join-Path $companionProjectDir "bin\$configuration\$framework"
$companionExe = Join-Path $outputDir "PasswordVault.PasskeyCompanion.exe"
$manifestOutput = Join-Path $outputDir "appxmanifest.generated.xml"

function Convert-ToAppxVersion {
    param([string]$Version)

    $segments = @($Version -split "\.")
    while ($segments.Count -lt 4) {
        $segments += "0"
    }

    return ($segments[0..3] -join ".")
}

$winapp = Get-Command winapp -ErrorAction SilentlyContinue
if (-not $winapp) {
    throw "The winapp CLI was not found. Install it first, then rerun this script."
}

$packageJsonPath = Join-Path $repoRoot "package.json"
$packageJson = Get-Content -Raw -Path $packageJsonPath | ConvertFrom-Json
$appxVersion = Convert-ToAppxVersion $packageJson.version

Write-Host "Building PasswordVault.PasskeyCompanion (Debug)..."
dotnet build $companionProject -c $configuration | Out-Host

if (-not (Test-Path $companionExe)) {
    throw "Companion executable not found: $companionExe"
}

$manifestContent = Get-Content -Raw -Path $manifestTemplate
$manifestContent = $manifestContent.Replace("__VERSION__", $appxVersion)
Set-Content -Path $manifestOutput -Value $manifestContent -Encoding UTF8

Write-Host "Applying a debug package identity..."
& $winapp.Source create-debug-identity $companionExe --manifest $manifestOutput | Out-Host

Write-Host ""
Write-Host "Debug identity created for:"
Write-Host "  $companionExe"
