# AGENTS.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Picker is a terminal-based file manager written in C# (.NET 10), inspired by ranger. It uses vim-style keybindings (h/j/k/l) for navigation and [Spectre.Console](https://spectreconsole.net/) for terminal UI rendering. Currently Windows-only.

## Build & Run

```pwsh
# Restore and build (debug)
dotnet build

# Run
dotnet run

# Build release package (self-contained single-file exe + installer zip)
pwsh scripts/build.ps1 -Configuration Release -Runtime win-x64
```

The build script publishes to `artifacts/publish/win-x64/` and creates `artifacts/picker-win-x64.zip` containing the exe plus install/uninstall scripts.

## Architecture

The app is a single-project console application (`picker.csproj`, namespace `Picker`).

**Entry point:** `Program/Main.cs` — sets UTF-8 console encoding (Windows), prints the banner, then hands off to `FileManager.Run()`.

**`Services/FileManager.cs`** — the core of the application. Contains the main loop (`Run`), input handling (`HandleInput`), directory loading, file opening (via shell execute), file creation, and all rendering logic. Rendering uses Spectre.Console `Layout` with three columns: a directory tree sidebar, the main file list, and a file preview pane.

**`Services/BookmarkService.cs`** — manages bookmark persistence. Bookmarks are stored as JSON at `%APPDATA%/picker/bookmarks.json`. Supports two types: `Project` and `Regular` (defined in `Models/BookmarkEntry.cs`).

**`Models/`** — simple data classes: `FileEntry` (name, path, size, modified, isDirectory) and `BookmarkEntry` (name, path, type enum).

**`scripts/`** — PowerShell scripts for packaging and installation:
- `build.ps1` — `dotnet publish` + zip packaging
- `install.ps1` — copies to `Program Files`, creates Start Menu/Desktop shortcuts (requires admin)
- `uninstall.ps1` — removes installed files and shortcuts

## Key Conventions

- Target framework is .NET 10 (`net10.0`) with nullable reference types enabled.
- The only NuGet dependency is `Spectre.Console`.
- `Program.cs` at the root is a stub with `using` statements; the real entry point is `Program/Main.cs`.
- Emoji icons are used when the console supports them; ASCII fallbacks are provided otherwise (see `GetFileIcon` / `CheckEmojiSupported` in `FileManager.cs`).
- Formatting uses tabs and CRLF line endings (see `.prettierrc`).
- Licensed under GPLv2 (`LICENSE.txt`).
