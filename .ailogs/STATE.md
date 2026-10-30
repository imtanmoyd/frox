# Project State

## Snapshot
- The MV3 extension and initial local-engine milestones are implemented.
- Existing deletions from the original FROX project are intentional worktree state and remain untouched.
- Engine dependencies were explicitly approved. No new Chrome permission was added.

## Works
- MV3 content script mounts one Shadow DOM widget in the top frame and skips restricted/browser/PDF pages.
- Animated, draggable bot and responsive chat panel; threads, attachments, markdown/code copy, stream stop/regenerate.
- Direct OpenAI-compatible and Anthropic streaming; extension API keys remain worker-only.
- Settings, popup, site blocklist, themes/skins, local error log, microphone level UI, and voice hotkey.
- FastAPI engine binds to loopback, fails closed without an extension ID, and limits CORS to that extension origin.
- Exact-origin `/bootstrap` returns a per-process token; all other API routes require `X-Frox-Token`.
- OpenRouter, Groq, Google AI Studio, and loopback Ollama have ordered fallback, capped retries, and 429 model rotation.
- SQLite stores chats, messages, facts, and site notes; FTS5 ranks local recall without model downloads or embedding dependencies.
- Memory API supports list, create/edit/delete, export, and forget-all; explicit chats can recall relevant local memories.
- Background probes and routes text-only chats through the engine, falls back to Mode A, and exposes engine/memory controls in popup and Settings.
- Engine tests: 24 passed; extension tests: 11 passed; typecheck, lint, Python compile, `pip check`, and MV3 build pass.

## Broken / Missing
- Voice capture has no transcription or speech playback.
- Page awareness, proactive suggestions, vector/semantic search, engine packaging, and desktop launcher are not implemented.
- No automated Chrome end-to-end verification; browser behavior still needs manual testing.
- `npm install` reported 6 dependency audit advisories; they have not been investigated.

## Next
1. Implement privacy-first page awareness only after defining consent, sensitive-domain, and blocklist behavior.
2. Ask before adding vector/embedding or speech dependencies; keep SQLite keyword recall as the current low-resource baseline.
3. Implement offline voice and engine packaging/desktop integration as separately verified milestones.

## Current validation
- `npm.cmd test`: 3 files, 11 tests passed.
- `npm.cmd run typecheck`, `npm.cmd run lint`, and `npm.cmd run build`: passed.
- `apps/engine`: 24 pytest cases passed; Python compile check and `pip check` passed.
