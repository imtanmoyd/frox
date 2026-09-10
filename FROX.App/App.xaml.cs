using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using FROX.Config;
using FROX.Core;
using WinForms = System.Windows.Forms;

namespace FROX.App;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;
    private OverlayWindow? _overlayWindow;
    private readonly IdleWatcher _idleWatcher = new();

    public static AppSettings Settings { get; } = new();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Catch unhandled exceptions on the UI thread
        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(
                $"Oops! FROX encountered a problem and will close.\n\n{args.Exception.GetType().Name}: {args.Exception.Message}",
                "FROX",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            args.Handled = true;
            Shutdown();
        };

        MainWindow = new MainWindow();
        MainWindow.Show();

        _overlayWindow = new OverlayWindow();
        _overlayWindow.Show();

        _idleWatcher.StateChanged += (_, state) =>
        {
            // Dispatch to UI thread since IdleWatcher timer runs on a thread-pool thread
            Dispatcher.Invoke(() =>
            {
                if (_overlayWindow is null)
                {
                    return;
                }

                _overlayWindow.SetMood(state == ActivityState.Idle ? FROX.Characters.CharacterAnimationState.Sleep : FROX.Characters.CharacterAnimationState.Idle);
            });
        };

        InitializeTrayIcon();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _idleWatcher.Dispose();
        base.OnExit(e);
    }

    private void InitializeTrayIcon()
    {
        // Load custom FROX icon for the system tray
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FROX.ico");
        var trayIcon = File.Exists(iconPath)
            ? new Icon(iconPath)
            : SystemIcons.Information;

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "FROX — Panda Companion",
            Icon = trayIcon
        };

        var contextMenu = new ContextMenuStrip();
        var showHideItem = new ToolStripMenuItem("Show / Hide overlay");
        showHideItem.Click += (_, _) => ToggleOverlay();

        var openChatItem = new ToolStripMenuItem("Open chat window");
        openChatItem.Click += (_, _) =>
        {
            MainWindow?.Show();
            MainWindow?.Activate();
        };

        var quitItem = new ToolStripMenuItem("Quit FROX");
        quitItem.Click += (_, _) => Shutdown();

        contextMenu.Items.Add(showHideItem);
        contextMenu.Items.Add(openChatItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(quitItem);
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                ToggleOverlay();
            }
        };
    }

    private void ToggleOverlay()
    {
        if (_overlayWindow is null)
        {
            return;
        }

        if (_overlayWindow.IsVisible)
        {
            _overlayWindow.Hide();
        }
        else
        {
            _overlayWindow.Show();
        }
    }
}
