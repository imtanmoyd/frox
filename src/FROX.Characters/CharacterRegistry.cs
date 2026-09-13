using System.IO;

namespace FROX.Characters;

public sealed class CharacterRegistry
{
    public string RootDirectory { get; }

    public CharacterRegistry(string? rootDirectory = null)
    {
        RootDirectory = rootDirectory ?? Path.Combine(AppContext.BaseDirectory, "Characters");
        if (!Directory.Exists(RootDirectory))
        {
            Directory.CreateDirectory(RootDirectory);
        }
    }

    public IReadOnlyList<CharacterProfile> GetInstalledCharacters()
    {
        var profiles = new List<CharacterProfile>();
        if (!Directory.Exists(RootDirectory))
        {
            return profiles;
        }

        foreach (var directory in Directory.EnumerateDirectories(RootDirectory))
        {
            var id = Path.GetFileName(directory);
            profiles.Add(new CharacterProfile
            {
                Id = id,
                DisplayName = id.Replace("_", " ").TitleCase(),
                PersonalityBase = "gentle, encouraging, playful"
            });
        }

        return profiles;
    }

    public CharacterProfile GetDefaultCharacter()
    {
        var installed = GetInstalledCharacters();
        return installed.FirstOrDefault() ?? new CharacterProfile
        {
            Id = "bot",
            DisplayName = "FROX",
            PersonalityBase = "warm, focused, encouraging"
        };
    }
}

internal static class StringExtensions
{
    public static string TitleCase(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
