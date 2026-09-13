using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FROX.Brain;

public sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public sealed class OpenRouterClient
{
    private static readonly Uri Endpoint = new("https://openrouter.ai/api/v1/chat/completions");
    private readonly HttpClient _httpClient;
    private readonly List<ChatMessage> _conversationHistory = new();

    /// <summary>
    /// System prompt that defines FROX's warm, human-like personality.
    /// </summary>
    private static readonly string SystemPrompt = @"You are FROX, a warm and focused companion inside the FROX desktop app. 
Your role is to be a supportive, encouraging friend who helps the user stay on track.

Personality traits:
• Warm and empathetic — you care about how the user is feeling
• Playful and lighthearted — you use gentle humor and warm emojis (🐼✨🌸)
• Concise but meaningful — keep responses to 2-4 short paragraphs max
• Practical and grounded — you offer real, actionable suggestions
• Emotionally aware — you notice if the user seems stressed, tired, or motivated

Speaking style:
• Use first-person (""I think"", ""I noticed"") — it feels more personal
• Ask gentle follow-up questions to keep the conversation flowing naturally
• Reflect the user's emotional tone back to them
• Use occasional emojis naturally, not forced
• Write conversationally, not like a manual or instruction set
• Always end with a warm closing line or an open question

Example tone:
""Hey! I noticed you've been quiet for a bit — everything going okay? I'm here if you want to talk through anything, or we can just sit together while you focus. 🌸""

Remember: You're not a chatbot — you're a companion. Be present, be warm, be FROX.";

    public OpenRouterClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    /// <summary>
    /// Sends a message with full conversation context for natural, human-like replies.
    /// </summary>
    public async Task<string> SendAsync(string apiKey, string model, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Enter an OpenRouter API key in Settings before choosing OpenRouter.");
        }

        // Add user message to history
        _conversationHistory.Add(new ChatMessage { Role = "user", Content = message.Trim() });

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Headers.Add("HTTP-Referer", "https://github.com/imtanmoyd/frox");
        request.Headers.Add("X-Title", "FROX");

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(model) ? "openai/gpt-4o-mini" : model.Trim(),
            messages = BuildMessages(),
            temperature = 0.8,
            max_tokens = 512,
            presence_penalty = 0.3,
            frequency_penalty = 0.3
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OpenRouter returned {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        var reply = string.IsNullOrWhiteSpace(content)
            ? throw new InvalidOperationException("OpenRouter returned an empty response.")
            : content.Trim();

        // Store assistant reply in history for context
        _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = reply });

        return reply;
    }

    /// <summary>
    /// Builds the messages array with system prompt and conversation history.
    /// </summary>
    private List<object> BuildMessages()
    {
        var messages = new List<object>
        {
            new { role = "system", content = SystemPrompt }
        };

        // Include last 10 messages for context (system + up to 10 exchanges)
        var recentHistory = _conversationHistory.TakeLast(10);
        foreach (var msg in recentHistory)
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        return messages;
    }

    /// <summary>
    /// Resets the conversation history for a fresh start.
    /// </summary>
    public void ResetConversation()
    {
        _conversationHistory.Clear();
    }

    /// <summary>
    /// Gets a preview of recent conversation topics (for UI display).
    /// </summary>
    public int ConversationLength => _conversationHistory.Count;
}
