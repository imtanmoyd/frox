using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Net.Http;
using FROX.Brain;

namespace FROX.App;

public partial class MainWindow : Window
{
    private readonly OpenRouterClient _openRouterClient = new();
    private readonly ConversationRouter _conversationRouter = new();

    public MainWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        UpdateChatStatus();
    }

    private void SetWindowIcon()
    {
        try
        {
            var assembly = typeof(App).Assembly;
            var names = assembly.GetManifestResourceNames();
            var icoName = names.FirstOrDefault(n => n.EndsWith("FROX.ico"));
            if (icoName is not null)
            {
                var uri = new Uri("pack://application:,,,/FROX.App;component/FROX.ico", UriKind.Absolute);
                Icon = System.Windows.Media.Imaging.BitmapFrame.Create(uri);
            }
        }
        catch
        {
            // Fall back to default window icon
        }
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        _openRouterClient.ResetConversation();
        PageTitle.Text = "Chat";
        WelcomeTitle.Text = "What would you like to focus on? 🐼";
        WelcomeText.Text = "FROX is offline-ready. Ask for a focus plan, a reset, or a quick check-in. I'm here to help! 🌸";
        MessageBox.Text = string.Empty;
        UpdateChatStatus();
    }

    private void Chat_Click(object sender, RoutedEventArgs e)
    {
        ShowChat();
    }

    private void Companion_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Companion";
        WelcomeTitle.Text = "Panda is with you 🐼";
        WelcomeText.Text = "The floating companion is active on your desktop. You can move it or hide it from the tray.";
    }

    private void Activity_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Activity";
        WelcomeTitle.Text = "A quiet activity view 🌿";
        WelcomeText.Text = "Your local activity summary will appear here as FROX learns your focus rhythm.";
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Settings";
        ChatView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
        Composer.Visibility = Visibility.Collapsed;
        ProviderBox.SelectedValue = App.Settings.Provider;
        ModelBox.Text = App.Settings.OpenRouterModel;
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    private void MessageBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            SendMessage();
            e.Handled = true;
        }
    }

    private async void SendMessage()
    {
        var message = MessageBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(message) || message == "Ask FROX anything...")
        {
            return;
        }

        MessageBox.IsEnabled = false;
        WelcomeTitle.Text = "🐼 Panda is thinking...";
        WelcomeText.Text = "Just a moment — I'm gathering my thoughts.";
        MessageBox.Text = string.Empty;

        try
        {
            if (App.Settings.Provider == "OpenRouter")
            {
                var response = await _openRouterClient.SendAsync(ApiKeyBox.Password, App.Settings.OpenRouterModel, message);
                WelcomeTitle.Text = "🐼 Panda says:";
                WelcomeText.Text = response;
            }
            else
            {
                // Offline mode — use the warm conversational router
                await Task.Delay(600); // Brief pause to feel more natural
                var reply = _conversationRouter.RouteMessage(message);
                WelcomeTitle.Text = "🐼 Panda says:";
                WelcomeText.Text = reply;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            WelcomeTitle.Text = "🐼 Panda couldn't connect";
            WelcomeText.Text = exception.Message + "\n\nTry switching to Offline mode in Settings, or check your API key and internet connection.";
        }
        finally
        {
            MessageBox.IsEnabled = true;
            UpdateChatStatus();
        }
    }

    private void ProviderBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProviderBox.SelectedValue is string provider)
        {
            App.Settings.Provider = provider;
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.Provider = ProviderBox.SelectedValue as string ?? "Offline";
        App.Settings.OpenRouterModel = ModelBox.Text.Trim();
        SettingsStatus.Text = App.Settings.Provider == "OpenRouter"
            ? "✅ OpenRouter is ready for this session. Your key remains in memory only."
            : "💻 Offline mode is active. Panda will respond with warm, local replies.";
    }

    private void ShowChat()
    {
        PageTitle.Text = "Chat";
        ChatView.Visibility = Visibility.Visible;
        SettingsView.Visibility = Visibility.Collapsed;
        Composer.Visibility = Visibility.Visible;
        WelcomeTitle.Text = "What would you like to focus on? 🐼";
        WelcomeText.Text = "FROX is offline-ready. Ask for a focus plan, a reset, or a quick check-in. I'm here to help! 🌸";
        UpdateChatStatus();
    }

    private void UpdateChatStatus()
    {
        // Show conversation length so user knows context is being remembered
        var msgCount = _openRouterClient.ConversationLength;
        var mode = App.Settings.Provider;
        var status = mode == "OpenRouter"
            ? $"☁️ Cloud mode · {msgCount} messages in this chat"
            : $"💻 Offline mode · {msgCount} messages in this chat";
        // Update the sidebar status text to reflect current mode
        if (msgCount > 0)
        {
            PageTitle.Text = $"Chat ({msgCount})";
        }
    }
}