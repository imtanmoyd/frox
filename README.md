# FROX

FROX is a lightweight Windows desktop companion built around a floating Panda overlay, idle detection, tray controls, and a simple offline-first chat shell.

## Features in this v1 build

- Transparent always-on-top overlay window
- Animated Panda sprite sheet using the provided PandaFree assets
- Tray icon with show/hide and chat actions
- Idle watcher scaffold for activity-aware state transitions
- Core project separation to match the build spec

## Run it

1. Open the solution in Visual Studio 2022 or use the .NET CLI.
2. Restore packages with `dotnet restore`.
3. Launch `FROX.App` with `dotnet run --project FROX.App`.

The asset files from `PandaFree/PandaFree` are copied into the output folder automatically for the app to use.
