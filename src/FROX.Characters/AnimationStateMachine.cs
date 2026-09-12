namespace FROX.Characters;

public enum CharacterAnimationState
{
    Idle,
    Sleep,
    Sit,
    Dance,
    ReactHappy,
    ReactConfused,
    Thinking
}

/// <summary>
/// Describes a state's playback: frames [0..LoopStart) play once as an intro,
/// then frames [LoopStart..FrameCount) loop forever.
/// Set LoopStart = 0 for a plain loop with no intro.
/// </summary>
public sealed record AnimationConfig(int FrameCount, int Fps, int LoopStart = 0);

public sealed class AnimationStateMachine
{
    // ---------------------------------------------------------------------------
    // Sprite sheet analysis (from montage inspection):
    //
    // panda-idle-sitting (frames 0-15):
    //   Full 16-frame wave/greeting loop — play all, loop from 0.
    //
    // panda-idle-sleeping (frames 0-15):
    //   0-11  → intro: waving → standing → sitting down
    //   12-15 → cartoony dissolve/fall-asleep transition
    //   15    → settled sleeping pose — hold this frame (loop on last frame)
    //
    // panda-thinking-forchatting (frames 0-15):
    //   0-8   → intro: wave/transition in
    //   9-11  → loop: thinking with question marks  (Thinking / ReactConfused)
    //   12    → lightbulb moment (part of ReactHappy intro)
    //   13-15 → happy payoff loop                  (ReactHappy)
    // ---------------------------------------------------------------------------
    private static readonly Dictionary<CharacterAnimationState, AnimationConfig> Configs = new()
    {
        [CharacterAnimationState.Idle]          = new AnimationConfig(FrameCount: 16, Fps: 10, LoopStart:  0),
        [CharacterAnimationState.Sit]           = new AnimationConfig(FrameCount: 16, Fps: 10, LoopStart:  0),
        [CharacterAnimationState.Dance]         = new AnimationConfig(FrameCount: 16, Fps: 12, LoopStart:  0),
        [CharacterAnimationState.Sleep]         = new AnimationConfig(FrameCount: 16, Fps:  6, LoopStart: 15),
        [CharacterAnimationState.Thinking]      = new AnimationConfig(FrameCount: 12, Fps:  8, LoopStart:  9),
        [CharacterAnimationState.ReactConfused] = new AnimationConfig(FrameCount: 12, Fps:  8, LoopStart:  9),
        [CharacterAnimationState.ReactHappy]    = new AnimationConfig(FrameCount: 16, Fps: 10, LoopStart: 13),
    };

    private AnimationConfig _config = Configs[CharacterAnimationState.Idle];

    public CharacterAnimationState CurrentState { get; private set; } = CharacterAnimationState.Idle;
    public int CurrentFrameIndex { get; private set; }
    public int TotalFrames => _config.FrameCount;

    public void SetState(CharacterAnimationState state)
    {
        if (!Configs.TryGetValue(state, out var config))
        {
            config = Configs[CharacterAnimationState.Idle];
        }

        CurrentState = state;
        _config = config;
        CurrentFrameIndex = 0; // always restart from frame 0 so the intro plays
    }

    /// <summary>
    /// Advances to the next frame. Intro frames (index &lt; LoopStart) play
    /// exactly once; after that playback wraps within [LoopStart..FrameCount).
    /// </summary>
    public void AdvanceFrame()
    {
        var next = CurrentFrameIndex + 1;

        if (next >= _config.FrameCount)
        {
            next = _config.LoopStart; // wrap to start of loop region
        }

        CurrentFrameIndex = next;
    }

    public int GetFrameRate() => _config.Fps;
}
