using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FROX.App;

public enum AttachmentKind { Image, Audio, Video, Document, Other }

/// <summary>A chat entry shown in the recent-chats sidebar.</summary>
public sealed class ChatItem : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New Chat";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    private string _preview = string.Empty;

    public string Preview
    {
        get => _preview;
        set { _preview = value; OnPropertyChanged(); }
    }

    private bool _isArchived;
    public bool IsArchived
    {
        get => _isArchived;
        set { _isArchived = value; OnPropertyChanged(); }
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public ObservableCollection<MessageItem> Messages { get; } = new();

    /// <summary>"h:mm tt" for today, "d MMM" otherwise.</summary>
    public string TimeLabel
    {
        get
        {
            var now = DateTime.Now;
            return UpdatedAt.Date == now.Date || UpdatedAt.Date == now.Date.AddDays(-1)
                ? UpdatedAt.ToString("h:mm tt")
                : UpdatedAt.ToString("d MMM");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>A single message bubble (user or assistant).</summary>
public sealed class MessageItem : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsUser { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;

    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    public ObservableCollection<AttachmentItem> Attachments { get; } = new();

    private bool _isTyping;
    public bool IsTyping
    {
        get => _isTyping;
        set { _isTyping = value; OnPropertyChanged(); }
    }

    public string TimeLabel => SentAt.ToString("h:mm tt");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>A file attached to a message or pending in the composer.</summary>
public sealed class AttachmentItem
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public AttachmentKind Kind { get; set; } = AttachmentKind.Other;
    public string SizeLabel { get; set; } = string.Empty;
    public ImageSource? Thumbnail { get; set; }

    public string KindGlyph => Kind switch
    {
        AttachmentKind.Image => "🖼️",
        AttachmentKind.Audio => "🎵",
        AttachmentKind.Video => "🎬",
        AttachmentKind.Document => "📄",
        _ => "📎",
    };

    public static string DescribeKind(AttachmentKind kind) => kind switch
    {
        AttachmentKind.Image => "Image",
        AttachmentKind.Audio => "Audio",
        AttachmentKind.Video => "Video",
        AttachmentKind.Document => "Document",
        _ => "File",
    };
}

/// <summary>A memory card shown in the Memory overlay.</summary>
public sealed class MemoryItem : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string TimeLabel => UpdatedAt.ToString("h:mm tt · d MMM");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public static class FileFormatting
{
    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:0.#} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:0.#} MB",
        >= 1_024 => $"{bytes / 1_024.0:0.#} KB",
        _ => $"{bytes} B",
    };

    public static AttachmentKind Classify(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        switch (extension)
        {
            case ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" or ".ico":
                return AttachmentKind.Image;
            case ".mp3" or ".wav" or ".m4a" or ".aac" or ".flac" or ".ogg":
                return AttachmentKind.Audio;
            case ".mp4" or ".webm" or ".mkv" or ".mov" or ".avi":
                return AttachmentKind.Video;
            case ".pdf" or ".doc" or ".docx" or ".txt" or ".md" or ".csv" or ".xlsx" or ".pptx":
                return AttachmentKind.Document;
            default:
                return AttachmentKind.Other;
        }
    }
}