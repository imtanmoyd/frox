# Architecture

## Folder map
- `packages/core`: platform-independent chat types, store, provider adapters, and utilities.
- `packages/ui`: reusable React widget, themes, and animations; no browser APIs.
- `apps/extension`: MV3 manifest, content script, service worker, popup, and options/voice pages.
- `apps/engine`: Python FastAPI local service; provider interfaces/router live under `src/frox_engine/providers`; local SQLite/FTS5 memory lives in `src/frox_engine/memory.py`.
- `apps/desktop`: reserved for a future Electron shell.
- `.ailogs`: concise project status, decisions, features, fixes, and session notes.

## Intended message flow
1. The top-frame content script mounts the reusable UI in a Shadow DOM and communicates through a platform adapter.
2. UI/core use platform interfaces; the extension adapter implements storage, messages, and hotkeys with Chrome APIs.
3. The service worker handles settings, protected storage, and direct provider streaming; provider keys are not sent to page scripts.
4. Persisted extension storage is authoritative so service-worker restarts do not lose conversations or settings.
5. The background probes the loopback engine for explicit text-only chat requests; Mode A remains available when the engine is absent or unsuitable. Image attachments use Mode A.
6. Memory is local SQLite with built-in FTS5; only explicit engine chats trigger recall and save conversation history/facts. No page content is proactively sent.
7. Engine `/bootstrap` is the only unauthenticated application route and returns a per-process token only to the exact configured extension Origin. All other API routes require `X-Frox-Token`.
8. The provider router uses OpenAI-compatible streaming for OpenRouter, Groq, Google AI Studio, and Ollama. Ordered fallback and model rotation are configurable; bounded retries avoid sending partial responses to a second provider.

## Decisions
- Use a monorepo boundary between reusable `core`/`ui` packages and host-specific shells.
- Use Shadow DOM to isolate host-page styles and widget styles.
- Keep the idle companion visually small and floating; bubble/menu remain separate accessible overlays anchored to its saved position. Character changes go through a narrow background message so provider secrets are not rewritten by content scripts.
- Use Vite with CRXJS to produce the MV3 extension bundle.
- `storage` is the only named permission; it supports settings, chat history, widget position, and local error logs.
- `<all_urls>` host access is required for the requested all-site content script and service-worker provider requests. It is not a named `permissions` entry.
- `commands` is a manifest capability, not a separate permission; it provides the push-to-talk shortcut.
- No `alarms` permission is planned; widget inactivity is tracked by the active content script.
- Voice capture will happen only after a user gesture in an extension-owned page/context and relies on browser microphone consent.
- `apps/engine` will bind only to `127.0.0.1:8765`; its API requires a random startup token and CORS must allow only the configured extension origin.
- Any engine runtime/test dependencies and any new extension permissions require user approval before they are added.
- Approved engine dependencies: FastAPI, Uvicorn, pydantic-settings, python-dotenv, httpx, and pytest. No vector/embedding or voice dependency is approved yet.
- Engine fails startup without a valid 32-character Chrome extension ID; `.env` is ignored and `.env.example` contains no credentials.
- Provider API keys are `SecretStr` settings from the ignored `.env`; status/settings routes expose only configured flags and model names.
- Ollama URLs are restricted to loopback; a bare `http://localhost:11434` is normalized to its OpenAI-compatible `/v1` endpoint.
- SQLite stores chats/messages, heuristic user facts, and site notes; FTS5 ranks local keyword matches and current-site notes. Recall is injected only into explicit engine chat prompts.
- Fact extraction is conservative, local-only, limited to user messages, and skips credential-like content. The memory API provides CRUD, export, and forget-all.
- Vector search and embedding/model downloads are deferred; SQLite/FTS5 uses no added memory dependency.
- The extension bootstraps and authenticates engine calls from the background worker, routes text-only chats when engine providers are configured, and preserves direct-provider fallback. Popup reports engine status; Settings provides memory controls.
- The pet overlay uses inline SVG and CSS only; its idle motion layers a slow body breathe, drifting float, antenna sway, orbit shimmer, and staggered sparkles. Action quick swap is transient UI state with six built-in moods/actions and an explicit random action. Character skins remain configurable in Settings. Motion pauses on hidden tabs and honors reduced-motion preferences. No remote animation/font assets or permissions are added.
- Engine routing derives only the hostname from the user-initiated chat sender URL. Site classification, proactive suggestions, transcription, and TTS are not implemented.
- The current voice page only captures mic levels locally; transcription and TTS are not implemented.

## Deferred platform details
- The `PlatformAdapter` separates reusable logic from `chrome.*` storage and runtime APIs.
- The API key is stored under a separate worker-only key; content scripts receive only a `hasApiKey` flag and non-secret settings.
- No remote code, external fonts, animation runtime, markdown parser, or CSS framework is planned.
