param(
    [string]$InstallDir = "$env:ProgramFiles\Picker",
    [switch]$CreateDesktopShortcut = $true
)

function Ensure-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "This installer needs to run as Administrator. Relaunching..."
        Start-Process -FilePath pwsh -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -InstallDir `"$InstallDir`"" -Verb RunAs
        exit
    }
}

Ensure-Admin

if (-not (Test-Path $InstallDir)) {
    New-Item -Path $InstallDir -ItemType Directory -Force | Out-Null
}

Write-Host "Installing to $InstallDir"
Copy-Item -Path (Join-Path $PSScriptRoot '..\artifacts\publish\win-x64\*') -Destination $InstallDir -Recurse -Force

# Create Start Menu shortcut
$W = New-Object -ComObject WScript.Shell
$programs = [Environment]::GetFolderPath('CommonPrograms')
$lnk = $W.CreateShortcut((Join-Path $programs 'Picker.lnk'))
$lnk.TargetPath = Join-Path $InstallDir 'picker.exe'
$lnk.IconLocation = $lnk.TargetPath
$lnk.Save()

if ($CreateDesktopShortcut) {
    $desktop = [Environment]::GetFolderPath('Desktop')
    $dlnk = $W.CreateShortcut((Join-Path $desktop 'Picker.lnk'))
    $dlnk.TargetPath = Join-Path $InstallDir 'picker.exe'
    $dlnk.IconLocation = $dlnk.TargetPath
    $dlnk.Save()
}

Write-Host "Installation complete."
Read-Host -Prompt "Press Enter to exit"
