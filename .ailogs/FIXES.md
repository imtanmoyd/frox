# Fixes

- 2026-10-31 | Typecheck failed assigning `window.setTimeout`'s numeric ID to Node's `Timeout` type | The browser widget uses the DOM timer API; typed its timer as `number`.
- 2026-10-31 | ESLint rejected CommonJS `require` in the root flat config | Renamed the config to `.mjs` and switched to ESM import/export.
- 2026-10-31 | Engine skeleton milestone | No additional bug fixes were required.
- 2026-10-31 | Provider routing milestone | No additional bug fixes were required.
- 2026-10-31 | Engine-only chat could be blocked by the widget when no extension API key was set | The UI treated direct-provider configuration as mandatory before sending; it now allows background engine routing and keeps Mode A fallback for unsupported/unavailable engine requests.
