# FROX

FROX is a dark, offline-first Windows desktop companion with persistent chats, memories, attachments, and an optional animated geometric bot.

## Design

- Dark, focused desktop interface built around `#0a0e1a`
- Frameless title bar, minimal chat sidebar, and clear message bubbles
- Simple square FROX bot with native WPF float, blink, thinking, and talking states
- Configurable bot visibility, corner position, size, and opacity
- Settings and memory panels with local persistence

## Run

```powershell
dotnet restore
dotnet run --project src\FROX.App
```

The application requires .NET 8 with the Windows desktop workload.
