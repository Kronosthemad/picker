param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [switch]$SelfContained = $true,
    [switch]$PublishSingleFile = $true
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $root

$projectFile = Get-ChildItem -Path .. -Filter "*.csproj" -Recurse -Depth 1 | Select-Object -First 1
if (-not $projectFile) {
    Write-Error "Could not find project file (.csproj) in repository root."
    exit 1
}

$projectPath = $projectFile.FullName
$publishDir = Join-Path $root "..\artifacts\publish\$Runtime"
$packageDir = Join-Path $root "..\artifacts\package"
$zipFile = Join-Path $root "..\artifacts\picker-$Runtime.zip"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
if (Test-Path $zipFile) { Remove-Item $zipFile -Force }

Write-Host "Publishing project: $projectPath"

$publishArgs = @(
    'publish',
    "$projectPath",
    '-c', $Configuration,
    '-r', $Runtime,
    '-o', $publishDir
)

if ($PublishSingleFile) { $publishArgs += '-p:PublishSingleFile=true' }
if ($SelfContained) { $publishArgs += '-p:SelfContained=true' }

$publish = & dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed"
    exit $LASTEXITCODE
}

Write-Host "Preparing package folder: $packageDir"
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

# Copy published files
Copy-Item -Path (Join-Path $publishDir '*') -Destination $packageDir -Recurse -Force

# Copy installer scripts and README
$scriptFiles = @('scripts\install.ps1','scripts\uninstall.ps1','README.md')
foreach ($f in $scriptFiles) {
    $src = Join-Path $root "..\$f"
    if (Test-Path $src) { Copy-Item -Path $src -Destination $packageDir -Force }
}

# Create zip
Write-Host "Creating zip: $zipFile"
Compress-Archive -Path (Join-Path $packageDir '*') -DestinationPath $zipFile -Force

Write-Host "Package created: $zipFile"
