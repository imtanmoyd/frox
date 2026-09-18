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
        // Single-instance enforcement with named mutex
        var mutex = new System.Threading.Mutex(true, "FROX-Desktop-App", out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running
            System.Windows.MessageBox.Show(
                "FROX is already running. Check your system tray for the existing instance.",
                "FROX",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Current.Shutdown();
            return;
        }

        // Keep mutex alive for the lifetime of the application
        AppDomain.CurrentDomain.ProcessExit += (s, args) => mutex?.Close();

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
                "FROX", "logs");
            Directory.CreateDirectory(folder);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var logFile = Path.Combine(folder, $"crash-{timestamp}.log");

            var logContent = $"""
            ============================================
            CRASH REPORT - {DateTime.Now:O}
            ============================================

            Exception Type: {exception.GetType().FullName}
            Message: {exception.Message}

            Stack Trace:
            {exception.StackTrace}

            """;

            if (exception.InnerException != null)
            {
                logContent += $"""
                Inner Exception: {exception.InnerException.GetType().FullName}
                Inner Message: {exception.InnerException.Message}
                Inner Stack Trace:
                {exception.InnerException.StackTrace}

                """;
            }

            // Add system info
            logContent += $"""
            System Info:
            OS Version: {Environment.OSVersion.VersionString}
            .NET Version: {Environment.Version}
            Machine Name: {Environment.MachineName}
            User Name: {Environment.UserName}
            App Version: {typeof(App).Assembly.GetName().Version}

            ============================================

            """;

            File.WriteAllText(logFile, logContent);

            // Also append to main crash log for chronological overview
            var mainLog = Path.Combine(folder, "crash.log");
            File.AppendAllText(mainLog, $"[{DateTime.Now:O}] {exception.GetType().Name}: {exception.Message}\n");
        }
        catch
        {
            // Logging must never prevent the application from reporting its error.
        }
    }
}