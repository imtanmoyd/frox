using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using FROX.App.Audio;
using FROX.Brain;
using FROX.Config;
using FROX.Data;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Brush = System.Windows.Media.Brush;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace FROX.App;

/// <summary>
/// Main chat window â€” the chat page, settings overlay and memory overlay
/// driven by the wireframe in MainWindow.xaml. State is persisted through
/// <see cref="FroxDbContext"/> (chats / messages / memories) and
/// <see cref="SecureSettingsStore"/> (profile, mode, provider, API key).
/// </summary>
public partial class MainWindow : Window
{
    private const string InputGhost = "Message FROXâ€¦";

    private readonly AppSettings _settings = App.Settings;
    private readonly SecureSettingsStore _settingsStore = new();
    private readonly FroxDbContext _db = new();
    private readonly MicRecorder _mic = new();
    private readonly ConversationRouter _router = new();
    private readonly OpenRouterClient _openRouter = new();

    private readonly List<ChatItem> _allChats = new();
    private readonly ObservableCollection<ChatItem> _recentChats = new();
    private readonly ObservableCollection<MessageItem> _messages = new();
    private readonly ObservableCollection<AttachmentItem> _pendingAttachments = new();
    private readonly ObservableCollection<MemoryItem> _memories = new();

    private ChatItem? _currentChat;
    private MemoryItem? _editingMemory;
    private Button? _activeRecordingButton;
    private bool _isSending;
    private bool _skipNextMicClick;
    private bool _sidebarCollapsed;

    // Bot character animation fields
    private readonly DispatcherTimer _thinkingEyeTimer = new();
    private readonly DispatcherTimer _talkingMouthTimer = new();
    private bool _mouthOpen;
    private const double LeftEyeBaseX = 18;
    private const double RightEyeBaseX = 34;
    private const double EyeBaseY = 22;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _settingsStore.Load(_settings);
        ApplySettingsToUi();

        LoadChats();
        LoadMemories();

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;

        // Push-to-talk: hold to record, release to stop.
        MicBarButton.PreviewMouseLeftButtonDown += MicBar_Press;
        MicBarButton.PreviewMouseLeftButtonUp += MicBar_Release;

        // Resource storyboards target named elements in the window namescope. They
        // are intentionally not started from the constructor because that can
        // race namescope initialization during application startup.
    }

    public ObservableCollection<ChatItem> RecentChats => _recentChats;
    public ObservableCollection<MessageItem> Messages => _messages;
    public ObservableCollection<AttachmentItem> PendingAttachments => _pendingAttachments;
    public ObservableCollection<MemoryItem> Memories => _memories;

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateEmptyState();
        ScrollToBottom();
        MessageInput.Focus();
        ((Storyboard)FindResource("FloatStoryboard")).Begin(this, true);
        ((Storyboard)FindResource("BlinkStoryboard")).Begin(this, true);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers is not (ModifierKeys.Control or ModifierKeys.Windows))
            return;

        switch (e.Key)
        {
            case Key.N:
                NewChatButton_Click(this, new RoutedEventArgs());
                break;
            case Key.OemComma:
                OpenSettingsButton_Click(this, new RoutedEventArgs());
                break;
            case Key.M:
                OpenMemoryButton_Click(this, new RoutedEventArgs());
                break;
            case Key.F:
                SearchBox.Focus();
                SearchBox.SelectAll();
                break;
            case Key.B:
                _settings.BotVisible = !_settings.BotVisible;
                ApplyBotSettings();
                break;
            case Key.S:
                ToggleSidebar_Click(this, new RoutedEventArgs());
                break;
            case Key.W:
            case Key.K:
                DeleteCurrentChat_Click(this, new RoutedEventArgs());
                break;
            case Key.Q:
                Close();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        SidebarPanel.Width = _sidebarCollapsed ? 48 : 256;
        SidebarContent.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        _settingsStore.Save(_settings);
        _mic.Dispose();

        // Stop bot character timers
        _thinkingEyeTimer.Stop();
        _talkingMouthTimer.Stop();
    }

    // ------------------------------------------------------------------ Data

    private void LoadChats()
    {
        foreach (var record in _db.GetChats(false))
        {
            var chat = new ChatItem
            {
                Id = record.Id,
                Title = record.Title,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                IsArchived = record.IsArchived,
                Preview = record.Preview
            };

            foreach (var message in _db.GetMessages(record.Id))
            {
                var item = new MessageItem
                {
                    Id = message.Id,
                    IsUser = message.Role == "user",
                    SentAt = message.SentAt,
                    Text = message.Content
                };
                foreach (var attachment in DeserializeAttachments(message.AttachmentsJson))
                {
                    item.Attachments.Add(attachment);
                }
                chat.Messages.Add(item);
            }

            _allChats.Add(chat);
        }

        if (_allChats.Count == 0)
        {
            CreateWelcomeChat();
        }
        else
        {
            var latest = _allChats.OrderByDescending(c => c.UpdatedAt).First();
            SelectChat(latest);
        }

        RefreshRecentChats();
        UpdateProfileUi();
    }

    private void CreateWelcomeChat()
    {
        var now = DateTime.Now;

        // A first-run chat is deliberately empty: it presents the landing-state
        // composer instead of injecting a message before the user starts.
        var chat = new ChatItem { Title = "New Chat", CreatedAt = now, UpdatedAt = now, Preview = "No messages yet" };
        _db.UpsertChat(chat.Id, chat.Title, chat.Preview, false);
        _allChats.Add(chat);
        SelectChat(chat);
    }

    private void SelectChat(ChatItem chat)
    {
        _currentChat = chat;
        _openRouter.ResetConversation(); // conversation context is per chat

        foreach (var item in _allChats)
        {
            item.IsActive = ReferenceEquals(item, chat);
        }

        _messages.Clear();
        foreach (var message in chat.Messages)
        {
            _messages.Add(message);
        }

        RefreshChatHeader();
        UpdateEmptyState();
        RefreshRecentChats();
        ScrollToBottom();
    }

    private void RefreshChatHeader()
    {
        if (_currentChat is null)
        {
            ChatTitleText.Text = "No chat selected";
            ChatSubtitleText.Text = string.Empty;
            return;
        }

        ChatTitleText.Text = _currentChat.Title;
        var count = _messages.Count;
        ChatSubtitleText.Text = $"{count} {(count == 1 ? "message" : "messages")} Â· {_currentChat.TimeLabel}";
    }

    private void UpdateEmptyState()
    {
        var isEmpty = _messages.Count == 0;
        EmptyStatePanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        ComposerHost.Margin = isEmpty
            ? new Thickness(24, 0, 24, 240)
            : new Thickness(24, 0, 24, 20);
        EmptyStatePanel.Margin = isEmpty
            ? new Thickness(0, 220, 0, 0)
            : new Thickness(0);
    }

    private void ScrollToBottom()
        => MessagesScroll.ScrollToEnd();

    private void RefreshRecentChats()
    {
        var query = SearchBox?.Text ?? string.Empty;
        IEnumerable<ChatItem> filtered = query.Trim().Length == 0
            ? _allChats.OrderByDescending(c => c.UpdatedAt)
            : _allChats.Where(c =>
                    c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.Preview.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.UpdatedAt);

        _recentChats.Clear();
        foreach (var chat in filtered)
        {
            _recentChats.Add(chat);
        }
    }

    // ------------------------------------------------------------------ Settings UI

    private void ApplySettingsToUi()
    {
        UpdateProfileUi();
        ApplyBotSettings();
    }

    private void ApplyBotSettings()
    {
        var size = Math.Clamp(_settings.BotSize, 40, 80);
        BotCharacterContainer.Width = size;
        BotCharacterContainer.Height = size;
        BotCharacterContainer.Opacity = Math.Clamp(_settings.BotOpacity, 0.6, 1.0);
        BotCharacterContainer.Visibility = _settings.BotVisible ? Visibility.Visible : Visibility.Collapsed;

        BotBottomRight.IsChecked = _settings.BotPosition == "bottom-right";
        BotBottomLeft.IsChecked = _settings.BotPosition == "bottom-left";
        BotTopRight.IsChecked = _settings.BotPosition == "top-right";
        BotTopLeft.IsChecked = _settings.BotPosition == "top-left";

        BotCharacterContainer.HorizontalAlignment =
            _settings.BotPosition.EndsWith("right", StringComparison.OrdinalIgnoreCase)
                ? System.Windows.HorizontalAlignment.Right
                : System.Windows.HorizontalAlignment.Left;
        BotCharacterContainer.VerticalAlignment =
            _settings.BotPosition.StartsWith("top", StringComparison.OrdinalIgnoreCase)
                ? System.Windows.VerticalAlignment.Top
                : System.Windows.VerticalAlignment.Bottom;
        BotCharacterContainer.Margin = _settings.BotPosition switch
        {
            "bottom-left" => new Thickness(24, 0, 0, 80),
            "top-right" => new Thickness(0, 80, 24, 0),
            "top-left" => new Thickness(24, 80, 0, 0),
            _ => new Thickness(0, 0, 24, 80)
        };
    }

    private void UpdateProfileUi()
    {
        var name = string.IsNullOrWhiteSpace(_settings.DisplayName) ? _settings.Username : _settings.DisplayName;
        ProfileNameText.Text = name;
        ProfileInitial.Text = name.Length == 0 ? "?" : name[..1].ToUpperInvariant();

        var modeLabel = _settings.Mode switch
        {
            "Online" => "Online",
            "Offline" => "Offline",
            _ => "Auto"
        };

        var providerLabel = ResolveProvider() == "OpenRouter"
            ? (string.IsNullOrWhiteSpace(_settings.OpenRouterModel) ? "OpenRouter" : _settings.OpenRouterModel)
            : "Local";

        StatusModeText.Text = modeLabel;
        StatusModelText.Text = providerLabel;
        SidebarModeText.Text = modeLabel;
        SidebarModelText.Text = providerLabel;
        SidebarStatusText.Text = $"{_allChats.Count} chats · {_memories.Count} memories";
        ProfileModeText.Text = $"{modeLabel} Â· {providerLabel}";
        StatusText.Text = $"{_allChats.Count} chats Â· {_memories.Count} memories";
    }

    private string ResolveProvider()
    {
        return _settings.Mode switch
        {
            "Online" => "OpenRouter",
            "Offline" => "Offline",
            // Auto: use the chosen provider whenever a key is available.
            _ => _settings.Provider == "OpenRouter" && !string.IsNullOrWhiteSpace(_settings.ApiKey)
                ? "OpenRouter"
                : "Offline"
        };
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(child);
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static string BuildPreview(MessageItem message)
    {
        var text = string.IsNullOrWhiteSpace(message.Text) ? "[attachments]" : message.Text;
        text = text.Replace("\r", " ").Replace("\n", " ");
        return text.Length > 90 ? text[..90] + "â€¦" : text;
    }

    private static string? SerializeAttachments(ObservableCollection<AttachmentItem> attachments)
    {
        if (attachments.Count == 0)
            return null;

        var dtos = attachments
            .Select(a => new AttachmentDto(a.FileName, a.FullPath, (int)a.Kind, a.SizeLabel))
            .ToList();
        return JsonSerializer.Serialize(dtos);
    }

    private static ObservableCollection<AttachmentItem> DeserializeAttachments(string? json)
    {
        var result = new ObservableCollection<AttachmentItem>();
        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            var dtos = JsonSerializer.Deserialize<List<AttachmentDto>>(json) ?? new List<AttachmentDto>();
            foreach (var dto in dtos)
            {
                result.Add(new AttachmentItem
                {
                    FileName = dto.FileName,
                    FullPath = dto.FullPath,
                    Kind = (AttachmentKind)dto.Kind,
                    SizeLabel = dto.SizeLabel
                });
            }
        }
        catch
        {
            // Corrupt attachment JSON is ignored â€” the message stays readable.
        }

        return result;
    }

    private static AttachmentItem MakeAttachment(string path)
    {
        var sizeLabel = "â€”";
        try
        {
            var info = new FileInfo(path);
            if (info.Exists)
            {
                sizeLabel = FileFormatting.FormatSize(info.Length);
            }
        }
        catch
        {
            // Best-effort size lookup.
        }

        return new AttachmentItem
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Kind = FileFormatting.Classify(path),
            SizeLabel = sizeLabel
        };
    }

    /// <summary>Small serializable snapshot of an on-disk attachment.</summary>
    private sealed record AttachmentDto(string FileName, string FullPath, int Kind, string SizeLabel);

    // ------------------------------------------------------------------ Chat actions

    private void ChatCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ChatItem chat })
        {
            SelectChat(chat);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        => RefreshRecentChats();

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        var chat = new ChatItem { Title = "New Chat", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
        _db.UpsertChat(chat.Id, chat.Title, "No messages yet", false);
        _allChats.Add(chat);
        SelectChat(chat);
        MessageInput.Focus();
    }

    private void DeleteCurrentChat_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChat is null)
            return;

        var answer = MessageBox.Show(this, $"Delete “{_currentChat.Title}” and all of its messages?",
            "Delete chat", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        var removed = _currentChat;
        _db.DeleteChatRecord(removed.Id);
        _allChats.Remove(removed);

        if (_allChats.Count == 0)
        {
            CreateWelcomeChat();
        }
        else if (ReferenceEquals(_currentChat, removed))
        {
            SelectChat(_allChats.OrderByDescending(c => c.UpdatedAt).First());
        }
        else
        {
            RefreshRecentChats();
        }

        UpdateProfileUi();
    }

    // ------------------------------------------------------------------ Window chrome

    private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        // Let the window control buttons handle their own clicks instead of dragging.
        if (e.OriginalSource is DependencyObject source && FindVisualParent<Button>(source) is not null)
            return;

        if (WindowState == WindowState.Maximized)
            return;

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove throws while the left button is not actually pressed.
        }
    }

    private void WindowControl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        switch (button.Tag?.ToString())
        {
            case "min":
                WindowState = WindowState.Minimized;
                break;
            case "max":
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                break;
            case "close":
                Close();
                break;
        }
    }

    // ------------------------------------------------------------------ Send pipeline

    private ChatItem EnsureActiveChat()
    {
        if (_currentChat is not null && _allChats.Contains(_currentChat))
            return _currentChat;

        var chat = new ChatItem { Title = "New Chat", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
        _db.UpsertChat(chat.Id, chat.Title, string.Empty, false);
        _allChats.Add(chat);
        SelectChat(chat);
        return chat;
    }

    private async void SendMessage()
    {
        if (_isSending)
            return;

        var rawText = MessageInput.Text == InputGhost ? string.Empty : MessageInput.Text;
        var text = rawText.Trim();
        _currentChat = EnsureActiveChat();

        if (text.Length == 0 && _pendingAttachments.Count == 0)
            return;

        MessageInput.Text = string.Empty;
        MessageInput.Foreground = (Brush)FindResource("TextPrimaryBrush");

        var userMessage = new MessageItem { IsUser = true, Text = text, SentAt = DateTime.Now };
        foreach (var attachment in _pendingAttachments)
        {
            userMessage.Attachments.Add(attachment);
        }
        _pendingAttachments.Clear();

        _messages.Add(userMessage);
        _currentChat.Messages.Add(userMessage);
        PersistMessage(userMessage);

        _currentChat.UpdatedAt = DateTime.Now;
        _currentChat.Title = PromoteTitle(_currentChat, text);
        _currentChat.Preview = BuildPreview(userMessage);
        _db.UpsertChat(_currentChat.Id, _currentChat.Title, _currentChat.Preview, _currentChat.IsArchived);

        UpdateEmptyState();
        RefreshChatHeader();
        RefreshRecentChats();
        ScrollToBottom();

        // Park a typing placeholder while the reply is computed.
        _isSending = true;
        SetBotThinking(); // Start thinking animation
        var typing = new MessageItem { IsUser = false, Text = "…", IsTyping = true, SentAt = DateTime.Now };
        _messages.Add(typing);
        _currentChat.Messages.Add(typing);
        ScrollToBottom();

        try
        {
            var reply = await GetReplyAsync(text);
            typing.Text = reply;
            typing.SentAt = DateTime.Now;
            typing.IsTyping = false;
            PersistMessage(typing);

            _currentChat.UpdatedAt = DateTime.Now;
            _currentChat.Preview = BuildPreview(typing);
            _db.UpsertChat(_currentChat.Id, _currentChat.Title, _currentChat.Preview, _currentChat.IsArchived);
        }
        catch (Exception ex)
        {
            typing.Text = $"⚠️ {ex.Message}";
            typing.SentAt = DateTime.Now;
            typing.IsTyping = false;
            PersistMessage(typing);
        }
        finally
        {
            _isSending = false;
            SetBotIdle(); // Return to idle state
            RefreshChatHeader();
            RefreshRecentChats();
            _ = Dispatcher.BeginInvoke(ScrollToBottom);
        }
    }

    private async Task<string> GetReplyAsync(string text)
    {
        if (ResolveProvider() == "OpenRouter")
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                return "I'd love to help with that, but I need an API key first — open ⚙ Settings and paste your OpenRouter key. 🔑\n\n(Or switch Provider to Offline and I'll stay right here, fully local)";
            }

            return await _openRouter.SendAsync(_settings.ApiKey, _settings.OpenRouterModel, text);
        }

        return await Task.Run(() => _router.RouteMessage(text));
    }

    private static string PromoteTitle(ChatItem chat, string text)
    {
        if (chat.Title != "New Chat" && chat.Title != "Welcome to FROX 👋")
            return chat.Title;

        if (text.Length == 0)
            return chat.Title;

        var candidate = text.Split('\n')[0].Trim();
        return candidate.Length > 40 ? candidate[..40] + "…" : candidate;
    }

    private void PersistMessage(MessageItem message)
    {
        if (_currentChat is null)
            return;

        _db.AddMessage(
            _currentChat.Id,
            message.IsUser ? "user" : "assistant",
            message.Text,
            message.SentAt,
            SerializeAttachments(message.Attachments));
    }

    // ------------------------------------------------------------------ Input & attachments

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            SendMessage();
            e.Handled = true;
        }
    }

    private void MessageInput_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (MessageInput.Text == InputGhost)
        {
            MessageInput.Text = string.Empty;
        }
        MessageInput.Foreground = (Brush)FindResource("TextPrimaryBrush");
    }

    private void MessageInput_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (MessageInput.Text.Length == 0)
        {
            MessageInput.Text = InputGhost;
            MessageInput.Foreground = (Brush)FindResource("TextFaintBrush");
        }
    }

    private void SendMessage_Click(object sender, RoutedEventArgs e)
        => SendMessage();

    private void AttachFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Attach media or documents",
            Multiselect = true,
            Filter = "Media & documents|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.ico;*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg;*.mp4;*.webm;*.mkv;*.mov;*.avi;*.pdf;*.doc;*.docx;*.txt;*.md;*.csv;*.xlsx;*.pptx|All files|*.*"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        foreach (var file in dialog.FileNames)
        {
            if (!string.IsNullOrWhiteSpace(file))
            {
                var attachment = MakeAttachment(file);
                // Generate thumbnail for image attachments
                attachment.GenerateThumbnail();
                _pendingAttachments.Add(attachment);
            }
        }
    }

    // ------------------------------------------------------------------ Workspace

    private void WorkspaceButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FROX");
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show(this, $"Couldn't open the workspace folder:\n{folder}",
                "FROX", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // ------------------------------------------------------------------ Settings overlay

    private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        UsernameBox.Text = _settings.Username;
        DisplayNameBox.Text = _settings.DisplayName;
        ModeCombo.SelectedIndex = _settings.Mode switch
        {
            "Offline" => 1,
            "Online" => 2,
            _ => 0
        };
        ProviderCombo.SelectedIndex = _settings.Provider == "OpenRouter" ? 1 : 0;
        ModelBox.Text = _settings.OpenRouterModel;
        ApiKeyBox.Password = _settings.ApiKey ?? string.Empty;
        ShowCharacterToggle.IsChecked = _settings.BotVisible;
        BotSizeSlider.Value = _settings.BotSize;
        BotOpacitySlider.Value = _settings.BotOpacity * 100;
        SettingsOverlay.Visibility = Visibility.Visible;
    }

    private void CloseSettingsOverlay_Click(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _settings.Username = UsernameBox.Text.Trim();
        _settings.DisplayName = string.IsNullOrWhiteSpace(DisplayNameBox.Text)
            ? _settings.Username
            : DisplayNameBox.Text.Trim();
        _settings.Mode = ModeCombo.SelectedIndex switch
        {
            1 => "Offline",
            2 => "Online",
            _ => "Auto"
        };
        _settings.Provider = ProviderCombo.SelectedIndex == 1 ? "OpenRouter" : "Offline";
        _settings.OpenRouterModel = ModelBox.Text.Trim();

        var key = ApiKeyBox.Password.Trim();
        _settings.ApiKey = key.Length == 0 ? null : key;
        _settings.BotVisible = ShowCharacterToggle.IsChecked == true;
        _settings.BotSize = (int)Math.Round(BotSizeSlider.Value);
        _settings.BotOpacity = BotOpacitySlider.Value / 100.0;
        _settings.BotPosition = BotBottomLeft.IsChecked == true ? "bottom-left"
            : BotTopRight.IsChecked == true ? "top-right"
            : BotTopLeft.IsChecked == true ? "top-left"
            : "bottom-right";

        _settingsStore.Save(_settings);
        ApplyBotSettings();
        UpdateProfileUi();
        SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    // ------------------------------------------------------------------ Memory overlay

    private void OpenMemoryButton_Click(object sender, RoutedEventArgs e)
    {
        LoadMemories();
        MemoryOverlay.Visibility = Visibility.Visible;
    }

    private void LoadMemories()
    {
        _memories.Clear();
        foreach (var record in _db.GetMemories())
        {
            _memories.Add(new MemoryItem
            {
                Id = record.Id,
                Title = record.Title,
                Content = record.Content,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt
            });
        }

        var noun = _memories.Count == 1 ? "memory" : "memories";
        MemorySubtitleText.Text = $"{_memories.Count} {noun} · FROX remembers what matters to you";
        UpdateProfileUi();
    }

    private void CloseMemoryOverlay_Click(object sender, RoutedEventArgs e)
    {
        CancelMemoryEdit();
        MemoryOverlay.Visibility = Visibility.Collapsed;
    }

    private void NewMemory_Click(object sender, RoutedEventArgs e)
    {
        _editingMemory = null;
        MemoryTitleBox.Text = string.Empty;
        MemoryContentBox.Text = string.Empty;
        MemoryEditorPanel.Visibility = Visibility.Visible;
        MemoryTitleBox.Focus();
    }

    private void EditMemory_Click(object sender, RoutedEventArgs e)
    {
        if (GetMemoryFromSender(sender) is not { } item)
            return;

        _editingMemory = item;
        MemoryTitleBox.Text = item.Title;
        MemoryContentBox.Text = item.Content;
        MemoryEditorPanel.Visibility = Visibility.Visible;
        MemoryTitleBox.Focus();
    }

    private void SaveMemory_Click(object sender, RoutedEventArgs e)
    {
        var id = _editingMemory?.Id ?? Guid.NewGuid();
        var title = MemoryTitleBox.Text.Trim();
        var content = MemoryContentBox.Text.Trim();

        if (title.Length == 0)
        {
            title = content.Length == 0
                ? "Untitled memory"
                : content[..Math.Min(32, content.Length)];
        }

        _db.SaveMemory(id, title, content);
        CancelMemoryEdit();
        LoadMemories();
    }

    private void CancelMemoryEdit_Click(object sender, RoutedEventArgs e)
        => CancelMemoryEdit();

    private void CancelMemoryEdit()
    {
        _editingMemory = null;
        MemoryEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void DeleteMemory_Click(object sender, RoutedEventArgs e)
    {
        if (GetMemoryFromSender(sender) is not { } item)
            return;

        var answer = MessageBox.Show(this, $"Delete “{item.Title}”?",
            "Delete memory", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        _db.DeleteMemory(item.Id);
        LoadMemories();
    }

    private static MemoryItem? GetMemoryFromSender(object sender)
    {
        if (sender is FrameworkElement { DataContext: MemoryItem item })
            return item;

        return (sender as FrameworkElement)?.Parent is ContextMenu { PlacementTarget: FrameworkElement { DataContext: MemoryItem menuItem } }
            ? menuItem
            : null;
    }

    // ------------------------------------------------------------------ Voice

    private void VoiceNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (_mic.IsRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording(button);
        }
    }

    private void StartRecording(Button button)
    {
        if (_mic.Start())
        {
            _activeRecordingButton = button;
            button.Content = "⏹";
            button.ToolTip = "Recording — click again to stop";
        }
        else
        {
            MessageBox.Show(this,
                "Couldn't access a microphone. Make sure one is connected and not already in use.",
                "FROX", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void StopRecording()
    {
        var path = _mic.Stop();
        _activeRecordingButton?.Dispatcher.Invoke(() =>
        {
            if (_activeRecordingButton is not null)
            {
                _activeRecordingButton.Content = _activeRecordingButton.Name == "MicBarButton" ? "🎙" : "🎤";
                _activeRecordingButton.ToolTip = _activeRecordingButton.Name == "MicBarButton"
                    ? "Microphone (hold to talk)"
                    : "Voice note (record)";
            }
        });
        _activeRecordingButton = null;

        if (path is not null)
        {
            _pendingAttachments.Add(MakeAttachment(path));
        }
    }

    // Push-to-talk: hold the bottom bar mic to record, release to stop.
    private void MicBar_Press(object sender, MouseButtonEventArgs e)
    {
        _skipNextMicClick = true;
        if (_mic.IsRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording(MicBarButton);
        }
    }

    private void MicBar_Release(object sender, MouseButtonEventArgs e)
    {
        _skipNextMicClick = true;
        if (_mic.IsRecording && _activeRecordingButton == MicBarButton)
        {
            StopRecording();
        }
    }

    private void MicButton_Click(object sender, RoutedEventArgs e)
    {
        if (_skipNextMicClick)
        {
            _skipNextMicClick = false;
            return;
        }

        if (_mic.IsRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording(MicBarButton);
        }
    }

    private void Attachment_Click(object sender, RoutedEventArgs e)
    {
        AttachFiles_Click(sender, e);
    }

    private void AttachmentMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button)
            return;

        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private void MemoryMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button)
            return;

        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private void SkillMenu_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "Choose a skill from your configured capabilities.\n\nAvailable: General assistant",
            "Skills", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void EditOptions_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "Attach files or use the microphone to add more context to your message.",
            "Message options",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ------------------------------------------------------------------ Bot character animation control

    private void SetBotThinking()
    {
        // Stop any existing talking animation
        _talkingMouthTimer.Stop();

        // Start thinking eye wander: move eyes side to side every 700ms
        _thinkingEyeTimer.Stop();
        _thinkingEyeTimer.Interval = TimeSpan.FromMilliseconds(700);
        int phase = 0;
        _thinkingEyeTimer.Tick += (s, e) =>
        {
            double off = phase switch { 1 => -4, 2 => 4, _ => 0 };
            Canvas.SetLeft(LeftEye, LeftEyeBaseX + off);
            Canvas.SetLeft(RightEye, RightEyeBaseX + off);
            phase = (phase + 1) % 3;
        };
        _thinkingEyeTimer.Start();

        // Set mouth to thinking (straight line)
        BotMouth.Data = Geometry.Parse("M 18,32 Q 24,32 30,32");
    }

    private void SetBotTalking()
    {
        // Stop thinking eye wander
        _thinkingEyeTimer.Stop();
        Canvas.SetLeft(LeftEye, LeftEyeBaseX);
        Canvas.SetLeft(RightEye, RightEyeBaseX);

        // Start talking mouth animation: open/close every 120ms
        _talkingMouthTimer.Stop();
        _talkingMouthTimer.Interval = TimeSpan.FromMilliseconds(120);
        _mouthOpen = false;
        _talkingMouthTimer.Tick += (s, e) =>
        {
            _mouthOpen = !_mouthOpen;
            BotMouth.Data = _mouthOpen
                ? Geometry.Parse("M 18,32 Q 24,36 30,32") // Down curve
                : Geometry.Parse("M 18,32 Q 24,28 30,32"); // Up curve
        };
        _talkingMouthTimer.Start();
    }

    private void SetBotIdle()
    {
        // Stop all timers and reset to idle state
        _thinkingEyeTimer.Stop();
        _talkingMouthTimer.Stop();
        Canvas.SetLeft(LeftEye, LeftEyeBaseX);
        Canvas.SetLeft(RightEye, RightEyeBaseX);
        BotMouth.Data = Geometry.Parse("M 18,32 Q 24,32 30,32"); // Idle mouth
    }
}
