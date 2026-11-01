# Build and deployment

The site now lives in `apps/web` and reuses the shared React widget from
`packages/ui`. Run it locally from the repository root:

```powershell
npm ci
npm run dev:web
```

Build the website with `npm run build:web`; Vite writes the static bundle to
`web-dist/`. The public chat is a browser client: it stores its conversations
and optional provider key in local storage and sends requests directly to the
provider the visitor selects. There is no Frox/Vercel chat API or shared
provider secret.

## Vercel

Import `imtanmoyd/frox` and use:

- **Project name:** `froxva` (if available)
- **Root directory:** repository root (`.`), not `docs` or `apps/web`
- **Build command:** `npm run build:web`
- **Output directory:** `web-dist`
- **Install command:** `npm install`

These settings are also recorded in the root `vercel.json`. Confirm or add
`froxva.vercel.app` under **Project → Settings → Domains** after the first
deployment. Vercel authentication and project/domain assignment still require
the account owner; no deployment has been created from this repository yet.

## Chrome extension

```powershell
npm run build:extension
```

Load the generated `dist/` folder from `chrome://extensions` with Developer
mode enabled. The extension adapter keeps provider keys in extension storage
and makes provider requests from its service worker.

## Desktop

The Electron shell is not implemented yet. See [apps/desktop/README.md](../apps/desktop/README.md)
for the intended adapter and security boundary.
