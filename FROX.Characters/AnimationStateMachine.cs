namespace FROX.Characters;

public enum CharacterAnimationState
{
    Idle,
    Sleep,
    Sit,
    Dance,
    ReactHappy,
    ReactConfused
}

public sealed class AnimationStateMachine
{
    private readonly Dictionary<CharacterAnimationState, int> _frameCounts = new()
    {
        [CharacterAnimationState.Idle] = 12,
        [CharacterAnimationState.Sleep] = 2,
        [CharacterAnimationState.Sit] = 1,
        [CharacterAnimationState.Dance] = 6,
        [CharacterAnimationState.ReactHappy] = 3,
        [CharacterAnimationState.ReactConfused] = 3
    };

    private readonly Dictionary<CharacterAnimationState, int> _fps = new()
    {
        [CharacterAnimationState.Idle] = 8,
        [CharacterAnimationState.Sleep] = 1,
        [CharacterAnimationState.Sit] = 1,
        [CharacterAnimationState.Dance] = 8,
        [CharacterAnimationState.ReactHappy] = 6,
        [CharacterAnimationState.ReactConfused] = 6
    };

    public CharacterAnimationState CurrentState { get; private set; } = CharacterAnimationState.Idle;
    public int CurrentFrameIndex { get; private set; }
    public int TotalFrames => _frameCounts.TryGetValue(CurrentState, out var count) ? count : 1;

    public void SetState(CharacterAnimationState state)
    {
        CurrentState = state;
        CurrentFrameIndex = 0;
    }

    public void AdvanceFrame()
    {
        var total = TotalFrames;
        CurrentFrameIndex = (CurrentFrameIndex + 1) % total;
    }

    public int GetFrameRate()
    {
        return _fps.TryGetValue(CurrentState, out var fps) ? fps : 8;
    }
}
