# Fixes

- 2026-10-31 | Typecheck failed assigning `window.setTimeout`'s numeric ID to Node's `Timeout` type | The browser widget uses the DOM timer API; typed its timer as `number`.
- 2026-10-31 | ESLint rejected CommonJS `require` in the root flat config | Renamed the config to `.mjs` and switched to ESM import/export.
- 2026-10-31 | Engine skeleton milestone | No additional bug fixes were required.
- 2026-10-31 | Provider routing milestone | No additional bug fixes were required.
- 2026-10-31 | Quick-swap menu could be clipped near the viewport bottom | Its vertical position did not reserve enough height | Clamp it using a conservative menu-height allowance and verify its bounds in the local browser preview.
- 2026-10-31 | Invisible quick-swap trigger could intercept clicks on the pet | The transparent hover button still accepted pointer input | Disable pointer events until hover/focus (keep it visible on touch-only devices).
- 2026-10-31 | Skin-save response could expose the provider key to a content script | The quick-swap route reused the extension Settings response | Return the updated settings with `apiKey` blank and only the non-secret `hasApiKey` flag.
- 2026-10-31 | Engine-only chat could be blocked by the widget when no extension API key was set | The UI treated direct-provider configuration as mandatory before sending; it now allows background engine routing and keeps Mode A fallback for unsupported/unavailable engine requests.
- 2026-10-31 | Pet action choices changed the menu label but not the pet animation | The selected action was stored but omitted from the pet button class list | Bind the action ID to the button class so the matching CSS animation runs; verified Dance and Poke in the widget preview.
