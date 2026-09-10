using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FROX.Brain;

public sealed class OpenRouterClient
{
    private static readonly Uri Endpoint = new("https://openrouter.ai/api/v1/chat/completions");
    private readonly HttpClient _httpClient;

    public OpenRouterClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };
    }

    public async Task<string> SendAsync(string apiKey, string model, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Enter an OpenRouter API key in Settings before choosing OpenRouter.");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Headers.Add("HTTP-Referer", "https://github.com/imtanmoyd/frox");
        request.Headers.Add("X-Title", "FROX");

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(model) ? "openai/gpt-4o-mini" : model.Trim(),
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are Panda, the gentle offline-first focus companion in FROX. Be concise, warm, and practical."
                },
                new
                {
                    role = "user",
                    content = message
                }
            }
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

        return string.IsNullOrWhiteSpace(content)
            ? throw new InvalidOperationException("OpenRouter returned an empty response.")
            : content;
    }
}
