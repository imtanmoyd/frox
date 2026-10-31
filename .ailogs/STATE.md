# Project State

## Snapshot
- MV3 extension and the initial local-engine milestones are implemented.
- Existing deletions from the original FROX project remain untouched; no new permissions or project dependencies were added for this UI milestone.

## Works
- Top-frame Shadow DOM widget, draggable bot, responsive chat, thread history, attachments, markdown/code copy, stream stop/regenerate.
- Overlay now has a floating orbiting pet with layered idle breathing, antenna sway, orbit shimmer, and staggered sparkles, plus hover pop and the animated action menu (idle, dance, sleep, poke, recharge, low battery, random); character skins remain in Settings.
- Pet is hidden while the full chat panel is open; quick-swap menu is positioned within the viewport, including near the bottom edge.
- Mode A direct OpenAI-compatible/Anthropic streaming and Mode B local-engine text chat with fallback.
- Settings links to Google AI Studio, GroqCloud, OpenRouter model catalog, and local Ollama; Google free-tier data-use caution is shown.
- FastAPI loopback engine with extension-origin token bootstrap, provider fallback, SQLite/FTS5 memory, and Settings controls.
- Ignored `apps/engine/.env` is ready for local Google credentials and Gemini model selection. No API key was copied from chat or sent to Google; rotate the key shared in chat before using it.
- `npm.cmd test`: 11 passed; typecheck, lint, and MV3 build pass after the layered pet animation polish. Engine pytest: 24 passed; `pip check` passes.
- Local Vite preview responds at `http://127.0.0.1:5173/options.html`; widget preview verified Dance/Poke animations, menu open/close, and bounds at 320×780. No actual provider call was made.

## Broken / Missing
- Local `.env` has no credentials or extension ID yet, and no engine/Ollama listener is running; live model chat remains unverified.
- Voice capture has no transcription or speech playback.
- Page awareness, proactive suggestions, semantic vector recall, engine packaging, and desktop launcher are not implemented.
- No automated Chrome end-to-end testing; manually load `dist/` and test with your own provider configuration.
- `npm install` reported 6 dependency audit advisories; they have not been investigated.

## Next
1. Load `dist/` in Chrome and test pet actions, provider setup, real chat stream, and Mode A fallback.
2. Keep site-awareness/privacy behavior explicit before implementing proactive suggestions.
3. Ask before adding vector/embedding or voice dependencies.

## Current validation
- Extension: 11 Vitest tests, typecheck, ESLint, and MV3 production build passed.
- Engine: 24 pytest tests and `pip check` passed.
- Preview: action animations and menu fit/open/close passed at 320×780; browser UI used a mock provider and synthetic stream.
