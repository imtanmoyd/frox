import { describe, expect, it } from "vitest";
import { createChatStore } from "./store";

describe("chat store", () => {
  it("creates a thread and selects it", () => {
    const store = createChatStore();
    const id = store.getState().createThread();

    expect(store.getState().activeThreadId).toBe(id);
    expect(store.getState().threads[0]?.title).toBe("New conversation");
  });

  it("titles a new thread from its first user message and supports pinning", () => {
    const store = createChatStore();
    const id = store.getState().createThread();
    store.getState().appendMessage(id, {
      id: "message-1",
      role: "user",
      content: "Help me plan a trip",
      createdAt: 123
    });
    store.getState().togglePinned(id);

    expect(store.getState().threads[0]?.title).toBe("Help me plan a trip");
    expect(store.getState().threads[0]?.pinned).toBe(true);
  });

  it("deletes the active thread and selects another existing thread", () => {
    const store = createChatStore();
    const first = store.getState().createThread();
    const second = store.getState().createThread();
    store.getState().deleteThread(second);

    expect(store.getState().activeThreadId).toBe(first);
  });

  it("keeps pinned conversations above newer unpinned conversations", () => {
    const store = createChatStore();
    const older = store.getState().createThread();
    store.getState().togglePinned(older);
    store.getState().createThread();

    expect(store.getState().threads[0]?.id).toBe(older);
  });
});
