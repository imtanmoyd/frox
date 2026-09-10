using System.Windows;

namespace FROX.App;

public partial class ChatPanelWindow : Window
{
    public ChatPanelWindow()
    {
        InitializeComponent();
        PromptBox.Text = "Ask Panda for a quick focus check-in or an encouraging start to your next session.";
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var text = PromptBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        System.Windows.MessageBox.Show($"Offline-ready reply: {text}", "FROX", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
