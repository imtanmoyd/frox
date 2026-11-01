# Project State

## Snapshot
- Frox is a multi-target workspace: reusable `packages/core` and `packages/ui`, a public web app, an MV3 extension, and an optional local Python engine.
- The website is an interactive product site, not a static-only landing page; it mounts the shared animated companion and multi-chat UI.

## Works
- `apps/web` is a Vite app with responsive landing page, Shadow DOM bot, chat threads, streaming, browser-local persistence, provider settings, and clear-data action.
- Web BYOK streaming goes directly from the browser to the selected provider; Frox/Vercel does not receive messages or API keys. localStorage is not encrypted and is documented as such.
- Shared chat uses a concise, emotionally considerate system instruction, sends up to 16 recent messages, and requests at most 512 output tokens in browser, extension, and local-engine provider paths.
- Web settings include OpenAI-compatible presets for OpenAI, OpenRouter, Groq, Google AI Studio, and local Ollama, plus Anthropic.
- Vercel root config builds `web-dist/` from the repository root; extension build remains independently available in `dist/`.
- Shared widget UI, state, provider adapters, and platform interfaces live in `packages/`; Chrome-specific APIs remain in `apps/extension`.
- MV3 extension, Mode A direct provider streaming, Mode B local-engine fallback, popup/options, and existing provider/memory routes remain intact.
- FastAPI loopback engine with extension-origin token bootstrap, provider fallback, SQLite/FTS5 memory, and settings controls is present.
- Electron host boundary is documented in `apps/desktop/README.md`; no Electron dependency/runtime has been added.
- `npm test`: 13 passed; typecheck, lint, web production build, and MV3 production build pass.
- Production-site browser test used a mocked SSE provider response; chat rendered, survived reload, stored locally, and the landing CTA opened the widget. Responsive checks passed at 320, 360, 390, 768, 1024, and 1440px.

## Broken / Missing
- Vercel account/project authorization is still required to publish at `froxva.vercel.app`; the repo now has root-based Vercel build settings, but no deployment was created.
- Live provider calls were not made. Direct browser chat requires a user's provider key (or reachable local Ollama) and provider CORS support.
- Website voice is disabled; the extension's voice recognition/speech playback and engine packaging remain incomplete.
- Page awareness, proactive suggestions, semantic vector recall, and desktop runtime/launcher remain unimplemented.
- There is no root `LICENSE`; choose a license before claiming formal open-source reuse terms.
- npm reported dependency audit advisories previously; they have not been investigated.

## Next
1. Sign in to Vercel, import the repo with root directory `.`, and confirm `froxva.vercel.app`.
2. Manually load the latest `dist/` in Chrome and test using a user-configured provider.
3. Keep browser chat keys local; add no server proxy without an explicit secret-handling design.
4. Obtain approval before adding Electron, voice, or vector/embedding dependencies.

## Current validation
- Web: `npm run build:web`; Vercel JSON parsed successfully. Browser-tested live preview with a mocked provider (no real key/model call); verified responsive layout and chat persistence.
- Extension: `npm run build:extension` passed.
- Shared workspace: `npm run typecheck`, `npm run lint`, and `npm test` passed (12 tests).
- Engine: `apps/engine/.venv/Scripts/python.exe -m pytest apps/engine/tests` — 24 passed, including system prompt forwarding, system-message exclusion from stored history, and output-token cap.
