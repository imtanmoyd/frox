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
}
