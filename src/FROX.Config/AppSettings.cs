using System.Text.Json.Serialization;

namespace FROX.Config;

public class AppSettings
{
    public string CharacterId { get; set; } = "panda";
    public string Theme { get; set; } = "Dark";
    public bool LaunchOnStartup { get; set; }
    public bool IdleTrackingEnabled { get; set; } = true;
    public bool MusicDetectionEnabled { get; set; } = true;
    public int IdleCheckSeconds { get; set; } = 5;
    public int AnimationFps { get; set; } = 8;
    public bool ShowBubble { get; set; } = true;

    // ---------- Profile ----------
    /// <summary>Login / handle shown in the sidebar footer and chat sending.</summary>
    public string Username { get; set; } = "Tanmoy";

    /// <summary>Friendly display name (e.g. "Tanmoy D.").</summary>
    public string DisplayName { get; set; } = "Tanmoy";

    // ---------- Mode selection (Auto / Offline / Online) ----------
    public string Mode { get; set; } = "Auto";

    // ---------- API configuration ----------
    /// <summary>AI provider id, e.g. "Offline" or "OpenRouter".</summary>
    public string Provider { get; set; } = "Offline";

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";

    public string OpenRouterModel { get; set; } = "openai/gpt-4o-mini";

    /// <summary>
    /// Session-only API key. Persisted (if the user opts in) with DPAPI encryption
    /// by <c>SecureSettingsStore</c> — never written to source, git or plain JSON.
    /// </summary>
    [JsonIgnore]
    public string? ApiKey { get; set; }
}
