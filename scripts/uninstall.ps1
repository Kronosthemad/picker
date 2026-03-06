param(
    [string]$InstallDir = "$env:ProgramFiles\Picker",
    [switch]$RemoveShortcuts = $true
)

function Ensure-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "This uninstaller needs to run as Administrator. Relaunching..."
        Start-Process -FilePath pwsh -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -InstallDir `"$InstallDir`"" -Verb RunAs
        exit
    }
}

Ensure-Admin

if (Test-Path $InstallDir) {
    Write-Host "Removing installation at $InstallDir"
    Remove-Item -Path $InstallDir -Recurse -Force
}

if ($RemoveShortcuts) {
    $programs = [Environment]::GetFolderPath('CommonPrograms')
    $lnk = Join-Path $programs 'Picker.lnk'
    if (Test-Path $lnk) { Remove-Item $lnk -Force }

    $desktop = [Environment]::GetFolderPath('Desktop')
    $dlnk = Join-Path $desktop 'Picker.lnk'
    if (Test-Path $dlnk) { Remove-Item $dlnk -Force }
}

Write-Host "Uninstallation complete."
Read-Host -Prompt "Press Enter to exit"
