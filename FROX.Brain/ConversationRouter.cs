namespace FROX.Brain;

public enum ConversationProvider
{
    Auto,
    Local,
    Cloud
}

public sealed class ConversationRouter
{
    public ConversationProvider PreferredProvider { get; set; } = ConversationProvider.Auto;

    public string RouteMessage(string message)
    {
        return $"Offline-ready reply: {message.Trim()}";
    }
}
