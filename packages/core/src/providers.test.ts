import { afterEach, describe, expect, it, vi } from "vitest";
import { OpenAICompatibleProvider } from "./providers";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("OpenAI-compatible provider", () => {
  it("streams text chunks and targets the compatible chat completions endpoint", async () => {
    const body = [
      'data: {"choices":[{"delta":{"content":"Hello"}}]}',
      'data: {"choices":[{"delta":{"content":" there"}}]}',
      "data: [DONE]"
    ].join("\n\n");
    const response = new Response(body, {
      status: 200,
      headers: { "Content-Type": "text/event-stream" }
    });
    const fetchMock = vi.fn(async () => response);
    vi.stubGlobal("fetch", fetchMock);
    const chunks: string[] = [];

    for await (const chunk of new OpenAICompatibleProvider().stream(
      [{ role: "user", content: "Hi" }],
      { apiKey: "test-key", model: "test-model", baseUrl: "http://localhost:11434" }
    )) chunks.push(chunk);

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:11434/v1/chat/completions",
      expect.objectContaining({ method: "POST" })
    );
    expect(chunks).toEqual(["Hello", " there"]);
  });

  it("reports a provider error response", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response("invalid key", { status: 401 })));

    await expect(async () => {
      for await (const _chunk of new OpenAICompatibleProvider().stream(
        [{ role: "user", content: "Hi" }],
        { apiKey: "bad-key", model: "test-model", baseUrl: "https://example.test/v1" }
      )) void _chunk;
    }).rejects.toThrow("Provider request failed (401): invalid key");
  });
});
