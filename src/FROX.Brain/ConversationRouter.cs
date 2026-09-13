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

    private static readonly string[] WarmGreetings = new[]
    {
        "Hey there! 🌸",
        "Right here with you! 🐼",
        "Hello! ✨",
        "I'm listening! 💫"
    };

    private static readonly string[] ThoughtfulResponses = new[]
    {
        "That's a great thought. Here's what I think — ",
        "I hear you on that. You know what comes to mind? ",
        "Thanks for sharing that with me. I'd say — ",
        "That makes a lot of sense. Let me offer this — "
    };

    private static readonly string[] WarmClosings = new[]
    {
        " How does that sound to you? 🌸",
        " What do you think? I'm here either way. 🐼",
        " Let me know if you'd like to go deeper on this. ✨",
        " I'm cheering for you — one step at a time. 💪",
    };

    private readonly Random _random = new();

    /// <summary>
    /// Routes a message and returns a warm, human-like response.
    /// In Local mode, generates thoughtful replies without AI.
    /// </summary>
    public string RouteMessage(string message)
    {
        var trimmed = message.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "I'm here when you're ready to talk. 🐼 Just type whatever's on your mind.";
        }

        // Check for common conversational patterns
        var lower = trimmed.ToLowerInvariant();

        if (IsGreeting(lower))
        {
            var greeting = WarmGreetings[_random.Next(WarmGreetings.Length)];
            return $"{greeting} It's good to see you! How's your day going so far? 🌟";
        }

        if (IsThanks(lower))
        {
            return "Of course! That's what I'm here for. 😊 Let me know if there's anything else you'd like to explore together! 🐼";
        }

        if (IsGoodbye(lower))
        {
            return "Take care! I'll be right here when you need me again. Wishing you a wonderful day ahead! 🌸✨";
        }

        if (IsFeelingQuery(lower))
        {
            var responses = new[]
            {
                "I'm feeling peaceful today — just watching the world go by and keeping you company. 🐼 How about you?",
                "I'm doing great, thank you for asking! I've been enjoying our time together. ✨ How are you feeling right now?",
                "Warm and content! 🌸 Just soaking in the moment. What's on your mind?",
            };
            return responses[_random.Next(responses.Length)];
        }

        // General thoughtful response
        var opener = ThoughtfulResponses[_random.Next(ThoughtfulResponses.Length)];
        var closer = WarmClosings[_random.Next(WarmClosings.Length)];
        var reflection = GenerateReflection(trimmed);

        return $"{opener}{reflection}{closer}";
    }

    private static bool IsGreeting(string lower) =>
        lower is "hi" or "hello" or "hey" or "hey there" or "yo" or "sup" or "what's up" or "good morning" or "good afternoon" or "good evening" or "howdy";

    private static bool IsThanks(string lower) =>
        lower.Contains("thank") || lower.Contains("thanks") || lower is "ty" or "thx";

    private static bool IsGoodbye(string lower) =>
        lower is "bye" or "goodbye" or "see you" or "later" or "cya" or "talk later" or "take care";

    private static bool IsFeelingQuery(string lower) =>
        lower.Contains("how are you") || lower.Contains("how's it going") || lower.Contains("how's frox") || lower.Contains("you okay");

    private static string GenerateReflection(string message)
    {
        // Simple reflection to make responses feel more human
        if (message.Length > 50)
        {
            return "that's quite a lot to unpack — and I appreciate you sharing it with me. " +
                   "The fact that you're thinking deeply about this says a lot about your character. " +
                   "Take it one piece at a time, and remember you don't have to figure everything out today.";
        }

        if (message.Length > 20)
        {
            return "I can see you've put some thought into this. " +
                   "That kind of reflection is really valuable. " +
                   "My gentle suggestion would be to trust your instincts — they're usually pointing you in the right direction.";
        }

        return "I appreciate you reaching out. Sometimes the smallest thoughts lead to the biggest breakthroughs. " +
               "I'm here to sit with you through it all.";
    }
}
