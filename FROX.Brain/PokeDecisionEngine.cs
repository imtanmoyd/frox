using FROX.Characters;

namespace FROX.Brain;

public sealed class PokeDecisionEngine
{
    private readonly Random _random = new();

    public string BuildPoke(CharacterProfile profile, int idleMinutes, bool musicPlaying, string activeAppCategory)
    {
        var name = profile.DisplayName;

        if (musicPlaying)
        {
            var musicPokes = new[]
            {
                $"{name} is dancing to the beat while you're in {activeAppCategory} — keep the groove going! 🎵🐼",
                $"{name} hears the music! Great vibes for focusing in {activeAppCategory}. 🎶✨",
                $"Looks like {name} is feeling the rhythm while you work in {activeAppCategory}. Dance break? 🕺🐼",
            };
            return musicPokes[_random.Next(musicPokes.Length)];
        }

        if (idleMinutes > 20)
        {
            var idlePokes = new[]
            {
                $"{name} notices you've been in {activeAppCategory} for a while. How about a tiny stretch break? 🧘 Your focus will thank you!",
                $"{name} is sending a gentle nudge — you've been at it for a bit. Stand up, breathe, and come back refreshed. 🌸",
                $"Pssst — {name} thinks you're doing great, but even bamboo bends sometimes. Take 60 seconds to reset. 🐼💚",
            };
            return idlePokes[_random.Next(idlePokes.Length)];
        }

        if (profile.Mood == CharacterMood.Excited)
        {
            var excitedPokes = new[]
            {
                $"{name} is feeling bright and excited today! The momentum is real — keep going! 🌟🐼",
                $"Sparks are flying! {name} is energized and ready to tackle anything with you. Let's go! ✨💪",
                $"{name} is practically bouncing with excitement. Whatever you're working on, it's going to be amazing! 🎉🐼",
            };
            return excitedPokes[_random.Next(excitedPokes.Length)];
        }

        if (profile.Mood == CharacterMood.Sleepy || idleMinutes > 10)
        {
            var sleepyPokes = new[]
            {
                $"{name} is getting a bit sleepy... but still keeping watch. One focused block at a time. 😴🐼",
                $"{name} is feeling the drowsy vibes too. Want a quick energy tip or a moment to rest? 🌙",
            };
            return sleepyPokes[_random.Next(sleepyPokes.Length)];
        }

        var calmPokes = new[]
        {
            $"{name} is keeping a low, calm watch. You're doing great — one focused block at a time. 🐼🌸",
            $"Just a quiet check-in from {name}. Everything's on track, and I'm right here when you need me. ✨",
            $"{name} is here, present and peaceful. How's your focus feeling today? 🐼💭",
        };
        return calmPokes[_random.Next(calmPokes.Length)];
    }
}
