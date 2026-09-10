using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Net.Http;
using FROX.Brain;

namespace FROX.App;

public partial class MainWindow : Window
{
    private readonly OpenRouterClient _openRouterClient = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Chat";
        WelcomeTitle.Text = "What would you like to focus on?";
        WelcomeText.Text = "FROX is offline-ready. Ask for a focus plan, a reset, or a quick check-in.";
        MessageBox.Text = string.Empty;
    }

    private void Chat_Click(object sender, RoutedEventArgs e)
    {
        ShowChat();
    }

    private void Companion_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Companion";
        WelcomeTitle.Text = "Panda is with you";
        WelcomeText.Text = "The floating companion is active on your desktop. You can move it or hide it from the tray.";
    }

    private void Activity_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Activity";
        WelcomeTitle.Text = "A quiet activity view";
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
        try
        {
            WelcomeTitle.Text = "Panda is thinking...";
            WelcomeText.Text = App.Settings.Provider == "OpenRouter"
                ? await _openRouterClient.SendAsync(ApiKeyBox.Password, App.Settings.OpenRouterModel, message)
                : $"Offline-ready reply: {message}";
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            WelcomeTitle.Text = "Panda could not connect";
            WelcomeText.Text = exception.Message;
        }
        finally
        {
            MessageBox.IsEnabled = true;
        }
        MessageBox.Text = string.Empty;
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
            ? "OpenRouter is ready for this session. Your key remains in memory only."
            : "Offline mode is active. No network calls will be made.";
    }

    private void ShowChat()
    {
        PageTitle.Text = "Chat";
        ChatView.Visibility = Visibility.Visible;
        SettingsView.Visibility = Visibility.Collapsed;
        Composer.Visibility = Visibility.Visible;
        WelcomeTitle.Text = "What would you like to focus on?";
        WelcomeText.Text = "FROX is offline-ready. Ask for a focus plan, a reset, or a quick check-in.";
    }
}