using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FROX.Characters;

namespace FROX.App;

public partial class OverlayWindow : Window
{
    private readonly AnimationStateMachine _stateMachine = new();
    private readonly DispatcherTimer _frameTimer = new();
    private readonly Dictionary<CharacterAnimationState, ImageSource> _sheets = new();
    private readonly Random _random = new();

    public OverlayWindow()
    {
        InitializeComponent();

        Width = 96;
        Height = 96;
        Background = System.Windows.Media.Brushes.Transparent;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        WindowStartupLocation = WindowStartupLocation.Manual;

        var workingArea = SystemParameters.WorkArea;
        Left = workingArea.Right - Width - 20;
        Top = workingArea.Bottom - Height - 32;

        LoadSpriteSheets();
        _stateMachine.SetState(CharacterAnimationState.Idle);
        UpdateFrame();

        _frameTimer.Interval = TimeSpan.FromMilliseconds(1000d / _stateMachine.GetFrameRate());
        _frameTimer.Tick += (_, _) =>
        {
            _stateMachine.AdvanceFrame();
            UpdateFrame();
        };
        _frameTimer.Start();
    }

    public void ShowPoke(string message)
    {
        BubbleText.Text = message;
        BubbleBorder.Visibility = Visibility.Visible;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(4)
        };
        timer.Tick += (_, _) =>
        {
            BubbleBorder.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }

    public void SetMood(CharacterAnimationState state)
    {
        _stateMachine.SetState(state);
        _frameTimer.Interval = TimeSpan.FromMilliseconds(1000d / _stateMachine.GetFrameRate());
        UpdateFrame();
    }

    private void LoadSpriteSheets()
    {
        var assetRoot = Path.Combine(AppContext.BaseDirectory, "PandaAssets");
        if (!Directory.Exists(assetRoot))
        {
            return;
        }

        var idle = SpriteSheetLoader.LoadSheet(Path.Combine(assetRoot, "panda-idle-sitting.png"));
        var sleeping = SpriteSheetLoader.LoadSheet(Path.Combine(assetRoot, "panda-idle-sleeping.png"));
        var thinking = SpriteSheetLoader.LoadSheet(Path.Combine(assetRoot, "panda-thinking-forchatting.png"));

        _sheets[CharacterAnimationState.Idle] = idle;
        _sheets[CharacterAnimationState.Sit] = idle;
        _sheets[CharacterAnimationState.Dance] = idle;
        _sheets[CharacterAnimationState.Sleep] = sleeping;
        _sheets[CharacterAnimationState.Thinking] = thinking;
        _sheets[CharacterAnimationState.ReactHappy] = thinking;
        _sheets[CharacterAnimationState.ReactConfused] = thinking;
    }

    private void UpdateFrame()
    {
        if (!_sheets.TryGetValue(_stateMachine.CurrentState, out var sheet))
        {
            if (!_sheets.TryGetValue(CharacterAnimationState.Idle, out sheet))
            {
                return; // No sheets loaded — nothing to display
            }
        }

        var source = SpriteSheetLoader.GetFrame(sheet, _stateMachine.CurrentFrameIndex, 64, 85);
        CharacterImage.Source = source;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}
