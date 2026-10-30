export interface StorageAdapter {
  get<T>(key: string): Promise<T | undefined>;
  set<T>(key: string, value: T): Promise<void>;
  remove(keys: string | string[]): Promise<void>;
  clear(): Promise<void>;
  watch<T>(key: string, listener: (value: T | undefined) => void): () => void;
}

export interface MessagingAdapter {
  send<TResponse>(message: ExtensionMessage): Promise<TResponse>;
  stream(
    message: ExtensionMessage,
    handlers: {
      onChunk: (text: string) => void;
      onDone: () => void;
      onError: (error: Error) => void;
    }
  ): () => void;
}

export interface HotkeyAdapter {
  onPushToTalk(handler: () => void, shortcut?: string): () => void;
}

export interface PlatformAdapter {
  storage: StorageAdapter;
  messaging: MessagingAdapter;
  hotkeys: HotkeyAdapter;
}

export type ExtensionMessage =
  | {
      type: "assistant:stream";
      conversationId?: string;
      messages: Array<{ role: string; content: string | LLMStreamContentPart[] }>;
    }
  | { type: "assistant:open-voice" }
  | { type: "assistant:widget-toggle"; enabled?: boolean }
  | { type: "assistant:widget-open" }
  | { type: "assistant:settings-get" }
  | { type: "assistant:settings-save"; settings: unknown }
  | { type: "assistant:settings-open" }
  | { type: "assistant:engine-status" }
  | { type: "assistant:memory-list"; query?: string; siteHost?: string }
  | { type: "assistant:memory-create"; kind: "fact" | "site_note"; content: string; siteHost?: string }
  | { type: "assistant:memory-update"; id: string; content?: string; siteHost?: string }
  | { type: "assistant:memory-delete"; id: string }
  | { type: "assistant:memory-export" }
  | { type: "assistant:memory-forget" }
  | { type: "assistant:site-toggle"; hostname: string }
  | { type: "assistant:widget-refresh" }
  | { type: "assistant:error-log"; error: { message: string; stack?: string } }
  | { type: "assistant:clear-data" };

export type LLMStreamContentPart =
  | { type: "text"; text: string }
  | { type: "image_url"; image_url: { url: string } };
