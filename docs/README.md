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

Import the private GitHub repository `imtanmoyd/froxva` and use:

- **Project name:** `frox` (if available)
- **Root directory:** repository root (`.`), not `docs` or `apps/web`
- **Production domain:** `frox.vercel.app`

The per-service install/build/output settings and routing are in the root
`vercel.json`. Confirm or add `frox.vercel.app` under **Project → Settings →
Domains** after the first deployment. Vercel authentication, private-repository
access, and domain assignment require the account owner.

The Vercel project declares two services. `app` serves the website and is the
only service targeted by the catch-all rewrite. `engine` is registered as an
internal FastAPI service and intentionally receives no public traffic: the
current engine requires a configured extension ID and loopback-oriented SQLite
storage, and is not yet a production cloud API. Do not add a rewrite to it
until cloud authentication, persistent storage, and its deployment-specific
configuration have been designed and reviewed.

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
