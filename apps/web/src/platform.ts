import { createProvider, MAX_CHAT_OUTPUT_TOKENS, prepareChatMessages } from "@frox/core";
import type {
  AssistantSettings,
  ExtensionMessage,
  HotkeyAdapter,
  LLMContentPart,
  LLMMessage,
  MessagingAdapter,
  PlatformAdapter,
  StorageAdapter
} from "@frox/core";

const STORAGE_PREFIX = "frox.web.";
const SETTINGS_KEY = "assistant:settings";
const ERROR_LOG_KEY = "assistant:errors";
type StorageListener = () => void;

class WebStorageAdapter implements StorageAdapter {
  private readonly listeners = new Map<string, Set<StorageListener>>();
  private listeningForStorageChanges = false;

  async get<T>(key: string): Promise<T | undefined> {
    const stored = localStorage.getItem(this.storageKey(key));
    return stored === null ? undefined : JSON.parse(stored) as T;
  }

  async set<T>(key: string, value: T): Promise<void> {
    localStorage.setItem(this.storageKey(key), JSON.stringify(value));
    this.notify(key);
  }

  async remove(keys: string | string[]): Promise<void> {
    for (const key of typeof keys === "string" ? [keys] : keys) {
      localStorage.removeItem(this.storageKey(key));
      this.notify(key);
    }
  }

  async clear(): Promise<void> {
    const keys: string[] = [];
    for (let index = 0; index < localStorage.length; index += 1) {
      const key = localStorage.key(index);
      if (key?.startsWith(STORAGE_PREFIX)) keys.push(key.slice(STORAGE_PREFIX.length));
    }
    for (const key of keys) await this.remove(key);
  }

  watch<T>(key: string, listener: (value: T | undefined) => void): () => void {
    if (!this.listeningForStorageChanges) {
      window.addEventListener("storage", (event) => {
        if (event.key === null) {
          this.listeners.forEach((_listeners, changedKey) => this.notify(changedKey));
        } else if (event.key.startsWith(STORAGE_PREFIX)) {
          this.notify(event.key.slice(STORAGE_PREFIX.length));
        }
      });
      this.listeningForStorageChanges = true;
    }
    const wrapped = () => listener(this.read<T>(key));
    const keyListeners = this.listeners.get(key) ?? new Set<StorageListener>();
    keyListeners.add(wrapped);
    this.listeners.set(key, keyListeners);
    return () => {
      keyListeners.delete(wrapped);
      if (keyListeners.size === 0) this.listeners.delete(key);
    };
  }

  private storageKey(key: string): string {
    return `${STORAGE_PREFIX}${key}`;
  }

  private read<T>(key: string): T | undefined {
    const stored = localStorage.getItem(this.storageKey(key));
    return stored === null ? undefined : JSON.parse(stored) as T;
  }

  private notify(key: string): void {
    this.listeners.get(key)?.forEach((listener) => listener());
  }
}

class WebMessagingAdapter implements MessagingAdapter {
  constructor(private readonly storage: WebStorageAdapter) {}

  async send<TResponse>(message: ExtensionMessage): Promise<TResponse> {
    switch (message.type) {
      case "assistant:settings-open":
        window.dispatchEvent(new Event("frox:web-settings"));
        return undefined as TResponse;
      case "assistant:widget-open":
        window.dispatchEvent(new Event("frox-widget-open"));
        return undefined as TResponse;
      case "assistant:settings-get":
        return await this.storage.get<AssistantSettings>(SETTINGS_KEY) as TResponse;
      case "assistant:settings-save":
        await this.storage.set(SETTINGS_KEY, message.settings);
        return undefined as TResponse;
      case "assistant:error-log": {
        const previous = await this.storage.get<Array<{ message: string; stack?: string; timestamp: number }>>(ERROR_LOG_KEY) ?? [];
        await this.storage.set(ERROR_LOG_KEY, [...previous, { ...message.error, timestamp: Date.now() }].slice(-30));
        return undefined as TResponse;
      }
      case "assistant:clear-data":
        await this.storage.clear();
        return undefined as TResponse;
      case "assistant:open-voice":
        throw new Error("Voice mode is not available on the website yet.");
      default:
        throw new Error(`The website does not support ${message.type}.`);
    }
  }

  stream(
    message: ExtensionMessage,
    handlers: {
      onChunk: (text: string) => void;
      onDone: () => void;
      onError: (error: Error) => void;
    }
  ): () => void {
    if (message.type !== "assistant:stream") {
      handlers.onError(new Error("The website received an unsupported chat request."));
      return () => undefined;
    }

    const controller = new AbortController();
    let cancelled = false;
    void this.runStream(message, controller.signal, handlers)
      .catch((error: unknown) => {
        if (cancelled || controller.signal.aborted) return;
        handlers.onError(error instanceof Error ? error : new Error(String(error)));
      });
    return () => {
      cancelled = true;
      controller.abort();
    };
  }

  private async runStream(
    message: Extract<ExtensionMessage, { type: "assistant:stream" }>,
    signal: AbortSignal,
    handlers: {
      onChunk: (text: string) => void;
      onDone: () => void;
    }
  ): Promise<void> {
    const settings = await this.storage.get<AssistantSettings>(SETTINGS_KEY);
    if (!settings) throw new Error("Choose an AI provider in Settings before starting a chat.");
    const localProvider = settings.provider === "openai" && ["localhost", "127.0.0.1", "[::1]"].includes(new URL(settings.baseUrl).hostname);
    if (!settings.apiKey && !localProvider) {
      throw new Error("Add your provider API key in Settings. It stays in this browser and is sent directly to that provider.");
    }
    if (message.messages.length === 0) throw new Error("There are no messages to send.");

    const conversation: LLMMessage[] = message.messages.flatMap((item): LLMMessage[] => {
      if (item.role !== "user" && item.role !== "assistant" && item.role !== "system") return [];
      if (typeof item.content === "string") return [{ role: item.role, content: item.content }];
      const content = item.content.flatMap((part): LLMContentPart[] => {
        if (part.type === "text") return [{ type: "text", text: part.text }];
        if (part.type === "image_url") return [{ type: "image_url", image_url: { url: part.image_url.url } }];
        return [];
      });
      return [{ role: item.role, content }];
    });
    if (conversation.length === 0) throw new Error("There are no valid messages to send.");

    const provider = createProvider(settings.provider);
    for await (const text of provider.stream(
      prepareChatMessages(conversation),
      {
        apiKey: settings.apiKey,
        model: settings.model,
        baseUrl: settings.baseUrl,
        maxOutputTokens: MAX_CHAT_OUTPUT_TOKENS,
        signal
      }
    )) {
      if (signal.aborted) return;
      handlers.onChunk(text);
    }
    if (!signal.aborted) handlers.onDone();
  }
}

class WebHotkeyAdapter implements HotkeyAdapter {
  onPushToTalk(): () => void {
    return () => undefined;
  }
}

const storage = new WebStorageAdapter();

export const webPlatform: PlatformAdapter = {
  storage,
  messaging: new WebMessagingAdapter(storage),
  hotkeys: new WebHotkeyAdapter()
};
