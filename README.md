# FROX

FROX is a lightweight Windows desktop companion built around a floating Panda overlay, idle detection, tray controls, and a simple offline-first chat shell.

## Features in this v1 build

- Transparent always-on-top overlay window
- Animated Panda sprite sheet using the PandaAssets sheets (idle-sitting, idle-sleeping, thinking-forchatting)
- Tray icon with show/hide and chat actions
- Idle watcher scaffold for activity-aware state transitions
- Core project separation to match the build spec

## Repository layout

```
FROX/                      <- repo root / "main folder"
├── src/
│   ├── FROX.App/          <- WPF app (entry point)
│   ├── FROX.Brain/        <- conversation routing + OpenRouter client
│   ├── FROX.Characters/   <- sprite sheet loading + animation state
│   ├── FROX.Config/       <- settings model + secure store
│   ├── FROX.Core/         <- hotkey manager + idle watcher
│   └── FROX.Data/         <- SQLite EF(-ish) context
├── assets/PandaAssets/    <- sprite sheet PNGs (source)
├── FROX.sln
└── FROX.App.exe ...       <- built app lands here after building
```

## Run it

1. Restore packages with `dotnet restore`.
2. Build the solution: `dotnet build FROX.sln` (or open `FROX.sln` in Visual Studio 2022).
3. Launch the app: run `D:\Projects\frox\FROX\FROX.App.exe` (the build output lands in the repo root), or use `dotnet run --project src\FROX.App`.

The sprite sheets from `assets/PandaAssets` are copied automatically into the output folder as `PandaAssets\` for the app to use.

## Notes

- `FROX.App` builds directly into the repository root (`D:\Projects\frox\FROX`), so the app and all its DLLs/`PandaAssets` sit in the main folder.
- These root-level build artifacts are ignored by git.
