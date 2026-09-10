using System.Windows;
using System.Windows.Input;

namespace FROX.App;

public partial class MainWindow : Window
{
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
        PageTitle.Text = "Chat";
        WelcomeTitle.Text = "What would you like to focus on?";
        WelcomeText.Text = "FROX is offline-ready. Ask for a focus plan, a reset, or a quick check-in.";
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
        WelcomeTitle.Text = "Simple by default";
        WelcomeText.Text = "Privacy, character, theme, and provider controls will live here.";
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

    private void SendMessage()
    {
        var message = MessageBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(message) || message == "Ask FROX anything...")
        {
            return;
        }

        WelcomeTitle.Text = "Panda says hello";
        WelcomeText.Text = $"Offline-ready reply: {message}";
        MessageBox.Text = string.Empty;
    }
}