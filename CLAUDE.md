# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Bitwarden For Command Palette is a Windows PowerToys Command Palette extension that integrates Bitwarden password manager functionality. It communicates with the local Bitwarden CLI (`bw`) to browse, search, copy, and manage vault items.

## Build Commands

```powershell
# Build for specific platform
dotnet build -p:Platform=x64      # Default
dotnet build -p:Platform=ARM64
dotnet build -p:Platform=x86

# Run tests
dotnet test

# Deploy (via Visual Studio)
dotnet deploy
```

## Solution Structure

Two projects in the solution:
- **BitwardenForCommandPalette** - Main extension (WinRT COM server)
- **BitwardenForCommandPalette.Tests** - xUnit test project

## Architecture

### Core Data Flow
```
Command Palette → IExtension → CommandProvider → DynamicListPage
                                      ↓
                              BitwardenCliService
                                      ↓
                              bw CLI (Process)
```

### Key Components

| Component | File | Purpose |
|-----------|------|---------|
| Extension Entry | `BitwardenForCommandPalette.cs` | Implements `IExtension`, returns `CommandProvider` |
| CommandProvider | `BitwardenForCommandPaletteCommandsProvider.cs` | Returns top-level commands, manages settings |
| Main Page | `BitwardenForCommandPalettePage.cs` | Dynamic list page for vault browsing |
| CLI Service | `Services/BitwardenCliService.cs` | Singleton wrapping all `bw` CLI interactions |
| Icon Service | `Services/IconService.cs` | Memory-cached icon fetching (200 entries max, domain blacklist) |

### Authentication Flow
1. Check API Key settings → set `BW_CLIENTID`/`BW_CLIENTSECRET` env vars
2. If no session key → prompt master password → `bw unlock --raw`
3. Cache session key for subsequent commands with `--session` flag

### COM Model
Out-of-process COM server via `Shmuelie.WinRTServer`. Single instance maintained and returned on each request. Lifecycle managed by `-RegisterProcessAsComServer` flag.

## Localization

UI strings are in `Strings/{locale}/Resources.resw` files. Access via `ResourceHelper` class using resource keys like `ActionCopy`, `UnlockPageTitle`, etc.

## Tests

Test project uses xUnit. Run with `dotnet test`. Tests cover:
- `SettingsManager` - Environment variable parsing
- `BitwardenStatus` - Status string parsing
- `BitwardenItem` - Subtitle generation for different item types

## Key Configuration Files

- `Directory.Packages.props` - Centralized NuGet package versions
- `Directory.Build.props` - Platform configuration
- `BitwardenForCommandPalette.csproj` - Project SDK, target framework (net9.0-windows10.0.26100.0)
