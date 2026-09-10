using System.Windows;
using FROX.Brain;

namespace FROX.App;

public partial class ChatPanelWindow : Window
{
    private readonly ConversationRouter _conversationRouter = new();

    public ChatPanelWindow()
    {
        InitializeComponent();
        PromptBox.Text = "Ask Panda anything... 🌸";
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var text = PromptBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text) || text == "Ask Panda anything... 🌸")
        {
            return;
        }

        SendButton.IsEnabled = false;
        PromptBox.Text = "🐼 Panda is thinking...";
        PromptBox.IsEnabled = false;

        try
        {
            // ChatPanel doesn't have access to the API key (password-protected in MainWindow),
            // so it always uses the warm offline conversation router
            await Task.Delay(400);
            var reply = _conversationRouter.RouteMessage(text);
            System.Windows.MessageBox.Show(reply, "🐼 Panda says...", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch
        {
            System.Windows.MessageBox.Show(
                "I'm here whenever you'd like to chat! 🌸 Just type whatever's on your mind.",
                "🐼 Panda says...",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            PromptBox.Text = "Ask Panda anything... 🌸";
            PromptBox.IsEnabled = true;
            SendButton.IsEnabled = true;
            PromptBox.Focus();
        }
    }
}
