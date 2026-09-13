using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using FROX.Config;

namespace FROX.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;

    public static AppSettings Settings { get; } = new();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Catch unhandled exceptions on the UI thread
        DispatcherUnhandledException += (_, args) =>
        {
            WriteStartupLog(args.Exception);
            System.Windows.MessageBox.Show(
                $"Oops! FROX encountered a problem and will close.\n\n{args.Exception.GetType().Name}: {args.Exception.Message}",
                "FROX",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            args.Handled = true;
            Shutdown();
        };

        try
        {
            MainWindow = new MainWindow();
            MainWindow.Show();
            InitializeTrayIcon();
        }
        catch (Exception ex)
        {
            WriteStartupLog(ex);
            System.Windows.MessageBox.Show(
                $"FROX could not start.\n\n{ex.GetType().Name}: {ex.Message}",
                "FROX",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    private void InitializeTrayIcon()
    {
        Icon trayIcon;
        try
        {
            var assembly = typeof(App).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("FROX.ico"));
            if (resourceName is not null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                trayIcon = stream is not null ? new Icon(stream) : SystemIcons.Information;
            }
            else
            {
                trayIcon = SystemIcons.Information;
            }
        }
        catch
        {
            trayIcon = SystemIcons.Information;
        }

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "FROX Companion",
            Icon = trayIcon
        };

        var contextMenu = new ContextMenuStrip();

        var showHideItem = new ToolStripMenuItem("Show / Hide bot");
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
            if (args.Button == MouseButtons.Left) ToggleOverlay();
        };
    }

    /// <summary>
    /// Toggles the visibility of the bot character in the main window.
    /// </summary>
    public void ToggleOverlay()
    {
        if (Current is App app && app.MainWindow is not null)
        {
            // Toggle the bot character visibility in the main window
            app.MainWindow.Dispatcher.Invoke(() =>
            {
                if (app.MainWindow is not MainWindow mainWindow)
                    return;

                var currentVisibility = mainWindow.BotCharacterContainer.Visibility;
                mainWindow.BotCharacterContainer.Visibility =
                    currentVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            });
        }
    }

    private static void WriteStartupLog(Exception exception)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FROX");
            Directory.CreateDirectory(folder);
            File.AppendAllText(
                Path.Combine(folder, "startup.log"),
                $"{DateTime.Now:O} {exception}\n\n");
        }
        catch
        {
            // Logging must never prevent the application from reporting its error.
        }
    }
}