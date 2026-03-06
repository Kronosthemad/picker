Picker simple ZIP installer

This repository includes a simple packaging workflow to produce a ZIP containing a self-contained publish of the app plus installer scripts.

How it works

- `scripts/build.ps1` - publishes the project using `dotnet publish` and packages the published files together with `install.ps1`, `uninstall.ps1`, and the repo `README.md` into `artifacts\picker-win-x64.zip`.

- `scripts/install.ps1` - installer script that copies the published files to `C:\Program Files\Picker`, creates Start Menu and Desktop shortcuts. It elevates to Administrator if needed.

- `scripts/uninstall.ps1` - removes the installed directory and shortcuts.

Quick usage

1. From PowerShell in repository root, run:

   pwsh scripts/build.ps1 -Configuration Release -Runtime win-x64

2. Open `artifacts\picker-win-x64.zip` and distribute it.

3. On a target machine, extract the zip, open PowerShell as Administrator, and run `.	ools\install.ps1` (adjust path as needed).

Notes

- The installer scripts assume a Windows target.
- For code signing, Inno Setup, or more advanced installers, replace the simple PowerShell scripts with an installer generator.
