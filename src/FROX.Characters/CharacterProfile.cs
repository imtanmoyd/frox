namespace FROX.Characters;

public enum CharacterMood
{
    Content,
    Sleepy,
    Excited,
    Concerned
}

public sealed class CharacterProfile
{
    public string Id { get; set; } = "panda";
    public string DisplayName { get; set; } = "Panda";
    public string PersonalityBase { get; set; } = "gentle, encouraging, playful";
    public CharacterMood Mood { get; set; } = CharacterMood.Content;
    public int Familiarity { get; set; }
    public int StreakDays { get; set; }
    public DateTimeOffset LastInteractionAt { get; set; } = DateTimeOffset.UtcNow;
    public string PreferredPokeStyle { get; set; } = "light";
}
