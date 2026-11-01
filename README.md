# Frox

Frox is a shared AI companion for the web, Chrome, and a future desktop shell.
The host-specific apps sit beside reusable packages so each target can choose
its own storage, messaging, and provider runtime without duplicating the UI.

## Repository map

```text
apps/
  web/          Vite landing site and browser-local chat
  extension/    Chrome Manifest V3 shell and service worker
  engine/       Optional local FastAPI engine
  desktop/      Electron host placeholder
packages/
  core/         Chat types, state, provider interfaces and adapters
  ui/           Shared React widget and styles
docs/           Deployment and contributor guidance
.ailogs/        AI-maintained project state and history
```

## Web site

```powershell
npm ci
npm run dev:web
npm run build:web
```

The public site's chat runs in the visitor's browser. Chats and provider
settings are saved in that browser's local storage; messages and API keys go
directly to the selected provider, not to Frox or Vercel. A provider key is
optional for local Ollama if the local service allows the site's origin.

Vercel builds from the repository root using `vercel.json`; set the project's
Root Directory to `.`. The output is `web-dist/`.

## Chrome extension

```powershell
npm run dev:extension
npm run build:extension
```

Load the generated `dist/` folder from `chrome://extensions` with Developer
mode enabled. The extension service worker owns provider keys and calls.

## Local engine

See [apps/engine/README.md](apps/engine/README.md) for Python setup and run
instructions. The loopback engine is optional; the extension can fall back to
direct provider mode.

## Desktop

[apps/desktop/README.md](apps/desktop/README.md) documents the planned host
contract. It will consume `@frox/core` and `@frox/ui` and provide an Electron
platform adapter; it is not an installable desktop app yet.
