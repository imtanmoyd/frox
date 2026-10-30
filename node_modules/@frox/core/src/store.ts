import { createStore } from "zustand/vanilla";
import type { ChatMessage, ChatThread } from "./types";

export interface ChatState {
  threads: ChatThread[];
  activeThreadId: string | null;
  setThreads: (threads: ChatThread[]) => void;
  setActiveThread: (threadId: string | null) => void;
  createThread: () => string;
  renameThread: (threadId: string, title: string) => void;
  togglePinned: (threadId: string) => void;
  deleteThread: (threadId: string) => void;
  appendMessage: (threadId: string, message: ChatMessage) => void;
  updateMessage: (threadId: string, messageId: string, update: Partial<ChatMessage>) => void;
}

function makeId(): string {
  if (typeof globalThis.crypto?.randomUUID === "function") return globalThis.crypto.randomUUID();
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}

export function createChatStore(initialThreads: ChatThread[] = []) {
  return createStore<ChatState>()((set) => ({
    threads: initialThreads,
    activeThreadId: initialThreads[0]?.id ?? null,
    setThreads: (threads) =>
      set((state) => ({
        threads: [...threads].sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.updatedAt - a.updatedAt),
        activeThreadId: threads.some((thread) => thread.id === state.activeThreadId)
          ? state.activeThreadId
          : (threads[0]?.id ?? null)
      })),
    setActiveThread: (activeThreadId) => set({ activeThreadId }),
    createThread: () => {
      const now = Date.now();
      const id = makeId();
      const thread: ChatThread = {
        id,
        title: "New conversation",
        pinned: false,
        createdAt: now,
        updatedAt: now,
        messages: []
      };
      set((state) => ({
        threads: [thread, ...state.threads].sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.updatedAt - a.updatedAt),
        activeThreadId: id
      }));
      return id;
    },
    renameThread: (threadId, title) =>
      set((state) => ({
        threads: state.threads.map((thread) => thread.id === threadId ? { ...thread, title: title.trim() || "New conversation" } : thread)
      })),
    togglePinned: (threadId) =>
      set((state) => ({
        threads: state.threads
          .map((thread) => thread.id === threadId ? { ...thread, pinned: !thread.pinned } : thread)
          .sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.updatedAt - a.updatedAt)
      })),
    deleteThread: (threadId) =>
      set((state) => {
        const threads = state.threads.filter((thread) => thread.id !== threadId);
        return {
          threads,
          activeThreadId: state.activeThreadId === threadId ? (threads[0]?.id ?? null) : state.activeThreadId
        };
      }),
    appendMessage: (threadId, message) => {
      set((state) => ({
        threads: state.threads
          .map((thread) => {
            if (thread.id !== threadId) return thread;
            const messages = [...thread.messages, message];
            const firstUserMessage = messages.find((item) => item.role === "user");
            const title = thread.title === "New conversation" && firstUserMessage
              ? firstUserMessage.content.slice(0, 40).trim() || "New conversation"
              : thread.title;
            return { ...thread, title, messages, updatedAt: message.createdAt };
          })
          .sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.updatedAt - a.updatedAt)
      }));
    },
    updateMessage: (threadId, messageId, update) =>
      set((state) => ({
        threads: state.threads.map((thread) => thread.id !== threadId ? thread : {
          ...thread,
          updatedAt: Date.now(),
          messages: thread.messages.map((message) => message.id === messageId ? { ...message, ...update } : message)
        })
      }))
  }));
}
