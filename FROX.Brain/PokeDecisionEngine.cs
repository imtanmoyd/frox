using FROX.Characters;

namespace FROX.Brain;

public sealed class PokeDecisionEngine
{
    public string BuildPoke(CharacterProfile profile, int idleMinutes, bool musicPlaying, string activeAppCategory)
    {
        if (musicPlaying)
        {
            return $"{profile.DisplayName} is dancing to the beat while {activeAppCategory} is in focus — keep the groove going.";
        }

        if (idleMinutes > 20)
        {
            return $"{profile.DisplayName} notices you've been in {activeAppCategory} for a while. Take a tiny stretch break and come back refreshed.";
        }

        if (profile.Mood == CharacterMood.Excited)
        {
            return $"{profile.DisplayName} is feeling bright today — keep the momentum going.";
        }

        return $"{profile.DisplayName} is keeping a low, calm watch. One focused block at a time.";
    }
}
