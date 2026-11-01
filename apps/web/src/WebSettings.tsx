import { useEffect, useState, type FormEvent } from "react";
import type { AssistantSettings, PlatformAdapter } from "@frox/core";

const SETTINGS_KEY = "assistant:settings";

const DEFAULT_SETTINGS: AssistantSettings = {
  provider: "openai",
  apiKey: "",
  hasApiKey: false,
  model: "gpt-4o-mini",
  baseUrl: "https://api.openai.com/v1",
  theme: "dark",
  skin: "nova",
  hotkey: "Alt+Shift+F",
  disabledSites: []
};

const PRESETS = {
  openai: { label: "OpenAI", provider: "openai", baseUrl: "https://api.openai.com/v1", model: "gpt-4o-mini" },
  openrouter: { label: "OpenRouter", provider: "openai", baseUrl: "https://openrouter.ai/api/v1", model: "openai/gpt-4o-mini" },
  groq: { label: "Groq", provider: "openai", baseUrl: "https://api.groq.com/openai/v1", model: "llama-3.1-8b-instant" },
  google: { label: "Google AI Studio", provider: "openai", baseUrl: "https://generativelanguage.googleapis.com/v1beta/openai", model: "gemini-2.5-flash" },
  ollama: { label: "Ollama (local)", provider: "openai", baseUrl: "http://localhost:11434/v1", model: "llama3.2" },
  anthropic: { label: "Anthropic", provider: "anthropic", baseUrl: "https://api.anthropic.com/v1", model: "claude-3-5-haiku-latest" }
} as const;

type PresetName = keyof typeof PRESETS;

function findPreset(settings: AssistantSettings): PresetName {
  if (settings.provider === "anthropic") return "anthropic";
  const hostname = new URL(settings.baseUrl).hostname;
  if (hostname === "openrouter.ai") return "openrouter";
  if (hostname === "api.groq.com") return "groq";
  if (hostname === "generativelanguage.googleapis.com") return "google";
  if (hostname === "localhost" || hostname === "127.0.0.1") return "ollama";
  return "openai";
}

export function WebSettings({ platform, onClose }: { platform: PlatformAdapter; onClose: () => void }) {
  const [settings, setSettings] = useState(DEFAULT_SETTINGS);
  const [preset, setPreset] = useState<PresetName>("openai");
  const [status, setStatus] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let mounted = true;
    void platform.storage.get<AssistantSettings>(SETTINGS_KEY).then((saved) => {
      if (!mounted || !saved) return;
      const merged = { ...DEFAULT_SETTINGS, ...saved };
      setSettings(merged);
      setPreset(findPreset(merged));
    }).catch((error: unknown) => {
      if (mounted) setStatus(error instanceof Error ? error.message : String(error));
    });
    return () => { mounted = false; };
  }, [platform]);

  const updateSetting = <K extends keyof AssistantSettings>(key: K, value: AssistantSettings[K]) => {
    setSettings((current) => ({ ...current, [key]: value }));
  };

  const selectPreset = (name: PresetName) => {
    const selected = PRESETS[name];
    setPreset(name);
    setSettings((current) => ({
      ...current,
      provider: selected.provider,
      baseUrl: selected.baseUrl,
      model: selected.model
    }));
  };

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    setStatus("");
    try {
      const base = new URL(settings.baseUrl);
      if ((base.protocol !== "https:" && base.protocol !== "http:") || base.username || base.password || base.search || base.hash) {
        throw new Error("Use an HTTP(S) provider URL without embedded credentials or query parameters.");
      }
      if (base.protocol === "http:" && !["localhost", "127.0.0.1", "[::1]"].includes(base.hostname)) {
        throw new Error("Use HTTPS for remote providers; HTTP is allowed only for a local service.");
      }
      if (settings.apiKey.length > 8192 || settings.model.trim().length === 0 || settings.model.length > 200) {
        throw new Error("Enter a model name and keep the API key within the supported size.");
      }
      await platform.storage.set(SETTINGS_KEY, { ...settings, model: settings.model.trim(), hasApiKey: Boolean(settings.apiKey) });
      setStatus("Saved on this device. Your next message goes directly to the selected provider.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
    } finally {
      setSaving(false);
    }
  };

  const clearData = async () => {
    if (!window.confirm("Clear Frox chats, settings, and saved API key from this browser?")) return;
    try {
      await platform.storage.clear();
      await platform.storage.set(SETTINGS_KEY, DEFAULT_SETTINGS);
      await platform.storage.set("assistant:threads", []);
      await platform.storage.set("assistant:position", {
        x: Math.max(12, window.innerWidth - 86),
        y: Math.max(12, window.innerHeight - 96)
      });
      setSettings(DEFAULT_SETTINGS);
      setPreset("openai");
      setStatus("Local Frox data cleared.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
    }
  };

  return (
    <div className="frox-web-settings-backdrop" onMouseDown={(event) => {
      if (event.target === event.currentTarget) onClose();
    }}>
      <section className="frox-web-settings" role="dialog" aria-modal="true" aria-labelledby="frox-web-settings-title">
        <header>
          <div><p>YOUR DEVICE, YOUR CHOICE</p><h2 id="frox-web-settings-title">Chat settings</h2></div>
          <button type="button" className="frox-web-settings-close" aria-label="Close settings" onClick={onClose}>×</button>
        </header>
        <p className="frox-web-privacy">
          This website has no Frox chat server. Chats and your key are stored in this browser (not encrypted); requests go directly from your browser to the provider you choose. Don’t save a key on a shared device.
        </p>
        <form onSubmit={(event) => void save(event)}>
          <label>
            Provider
            <select value={preset} onChange={(event) => selectPreset(event.target.value as PresetName)}>
              {Object.entries(PRESETS).map(([key, value]) => <option key={key} value={key}>{value.label}</option>)}
            </select>
          </label>
          <label>
            Model
            <input value={settings.model} onChange={(event) => updateSetting("model", event.target.value)} autoComplete="off" required />
          </label>
          <label>
            Provider API URL
            <input value={settings.baseUrl} onChange={(event) => updateSetting("baseUrl", event.target.value)} autoComplete="url" required />
          </label>
          <label>
            API key <span className="frox-web-optional">{preset === "ollama" ? "optional for local Ollama" : "saved only in this browser"}</span>
            <input
              type="password"
              value={settings.apiKey}
              onChange={(event) => updateSetting("apiKey", event.target.value)}
              autoComplete="new-password"
              spellCheck={false}
            />
          </label>
          <p className="frox-web-provider-note">
            Free-tier access and browser CORS support depend on your provider. Ollama must allow this site as an origin. Frox does not proxy or receive your messages or key.
          </p>
          {status && <p className="frox-web-settings-status" role="status">{status}</p>}
          <div className="frox-web-settings-actions">
            <button type="button" className="frox-web-clear" onClick={() => void clearData()}>Clear local data</button>
            <button type="submit" className="frox-web-save" disabled={saving}>{saving ? "Saving…" : "Save settings"}</button>
          </div>
        </form>
      </section>
    </div>
  );
}
