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

public sealed class AnimationStateMachine
{
    private readonly Dictionary<CharacterAnimationState, int> _frameCounts = new()
    {
        [CharacterAnimationState.Idle] = 16,
        [CharacterAnimationState.Sleep] = 16,
        [CharacterAnimationState.Sit] = 1,
        [CharacterAnimationState.Dance] = 16,
        [CharacterAnimationState.ReactHappy] = 16,
        [CharacterAnimationState.ReactConfused] = 16,
        [CharacterAnimationState.Thinking] = 16
    };

    private readonly Dictionary<CharacterAnimationState, int> _fps = new()
    {
        [CharacterAnimationState.Idle] = 10,
        [CharacterAnimationState.Sleep] = 2,
        [CharacterAnimationState.Sit] = 1,
        [CharacterAnimationState.Dance] = 12,
        [CharacterAnimationState.ReactHappy] = 10,
        [CharacterAnimationState.ReactConfused] = 10,
        [CharacterAnimationState.Thinking] = 8
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
