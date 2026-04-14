$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$companionProjectDir = Join-Path $repoRoot "windows-passkey-plugin\PasswordVault.PasskeyCompanion"
$companionProject = Join-Path $companionProjectDir "PasswordVault.PasskeyCompanion.csproj"
$manifestTemplate = Join-Path $companionProjectDir "appxmanifest.xml"
$assetsDir = Join-Path $companionProjectDir "Assets"
$configuration = "Release"
$framework = "net10.0-windows10.0.19041.0"
$publishDir = Join-Path $companionProjectDir "bin\$configuration\$framework"
$outputDir = Join-Path $companionProjectDir "bin\$configuration\msix"
$manifestOutput = Join-Path $outputDir "appxmanifest.generated.xml"
$certificatePfx = Join-Path $outputDir "PasswordVault.PasskeyCompanion.DevCert.pfx"
$certificateCer = Join-Path $outputDir "PasswordVault.PasskeyCompanion.DevCert.cer"
$publisher = "CN=PasswordVaultDev"
$certificatePassword = "password"

function Convert-ToAppxVersion {
    param([string]$Version)

    $segments = @($Version -split "\.")
    while ($segments.Count -lt 4) {
        $segments += "0"
    }

    return ($segments[0..3] -join ".")
}

if (-not (Test-Path $companionProject)) {
    throw "Companion project not found: $companionProject"
}

if (-not (Test-Path $manifestTemplate)) {
    throw "Manifest template not found: $manifestTemplate"
}

if (-not (Test-Path $assetsDir)) {
    throw "Companion assets directory not found: $assetsDir"
}

$winapp = Get-Command winapp -ErrorAction SilentlyContinue
if (-not $winapp) {
    throw "The winapp CLI was not found. Install it first, then rerun this script."
}

$packageJsonPath = Join-Path $repoRoot "package.json"
$packageJson = Get-Content -Raw -Path $packageJsonPath | ConvertFrom-Json
$appxVersion = Convert-ToAppxVersion $packageJson.version

Write-Host "Building PasswordVault.PasskeyCompanion..."
dotnet build $companionProject -c $configuration | Out-Host

if (-not (Test-Path $publishDir)) {
    throw "Companion build output not found: $publishDir"
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$manifestContent = Get-Content -Raw -Path $manifestTemplate
$manifestContent = $manifestContent.Replace("__VERSION__", $appxVersion)
Set-Content -Path $manifestOutput -Value $manifestContent -Encoding UTF8

$existingCertificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $publisher } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $existingCertificate) {
    Write-Host "Creating a local development signing certificate..."
    $existingCertificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $publisher `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -HashAlgorithm "SHA256" `
        -NotAfter (Get-Date).AddYears(3)
}

$securePassword = ConvertTo-SecureString -String $certificatePassword -AsPlainText -Force
Export-PfxCertificate -Cert $existingCertificate -FilePath $certificatePfx -Password $securePassword | Out-Null
Export-Certificate -Cert $existingCertificate -FilePath $certificateCer | Out-Null

$existingMsix = Get-ChildItem -Path $outputDir -Filter *.msix -ErrorAction SilentlyContinue
if ($existingMsix) {
    $existingMsix | Remove-Item -Force
}

Push-Location $outputDir
try {
    Write-Host "Packing companion app into MSIX..."
    & $winapp.Source pack $publishDir `
        --manifest $manifestOutput `
        --cert $certificatePfx `
        --cert-password $certificatePassword | Out-Host
}
finally {
    Pop-Location
}

$msix = Get-ChildItem -Path $outputDir -Filter *.msix |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $msix) {
    throw "winapp pack completed, but no .msix file was found in $outputDir"
}

Write-Host ""
Write-Host "MSIX created:"
Write-Host "  $($msix.FullName)"
Write-Host ""
Write-Host "Development certificate exported:"
Write-Host "  PFX: $certificatePfx"
Write-Host "  CER: $certificateCer"
