using Microsoft.Data.Sqlite;

namespace FROX.Data;

/// <summary>Lightweight persisted state for one chat conversation.</summary>
public sealed record ChatRecord(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, bool IsArchived, string Preview);

/// <summary>A single chat message; <c>Role</c> is "user" or "assistant".</summary>
public sealed record MessageRecord(Guid Id, Guid ChatId, string Role, string Content, DateTime SentAt, string? AttachmentsJson);

/// <summary>A memory card shown in the Memory overlay.</summary>
public sealed record MemoryRecord(Guid Id, string Title, string Content, DateTime CreatedAt, DateTime UpdatedAt);

/// <summary>
/// SQLite-backed store for chats, messages and memories.
/// Database lives in %LocalAppData%\FROX\frox.db so it survives app rebuilds.
/// </summary>
public sealed class FroxDbContext
{
    private readonly string _databasePath;

    public FroxDbContext(string? databasePath = null)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FROX");
        Directory.CreateDirectory(directory);
        _databasePath = databasePath ?? Path.Combine(directory, "frox.db");
        EnsureCreated();
    }

    public string DatabasePath => _databasePath;

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? AppContext.BaseDirectory);

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS CharacterProfiles (
                Id TEXT PRIMARY KEY,
                DisplayName TEXT NOT NULL,
                Familiarity INTEGER NOT NULL DEFAULT 0,
                Mood TEXT NOT NULL DEFAULT 'Content',
                LastInteractionAt TEXT NOT NULL,
                StreakDays INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS ActivityLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                ForegroundAppCategory TEXT NOT NULL,
                IdleMinutes INTEGER NOT NULL,
                MusicPlaying INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Chats (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Preview TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsArchived INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Messages (
                Id TEXT PRIMARY KEY,
                ChatId TEXT NOT NULL,
                Role TEXT NOT NULL,
                Content TEXT NOT NULL,
                SentAt TEXT NOT NULL,
                AttachmentsJson TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Messages_ChatId ON Messages(ChatId);

            CREATE TABLE IF NOT EXISTS Memories (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    // ------------------------------------------------------------ Chats

    public List<ChatRecord> GetChats(bool includeArchived = false)
    {
        var result = new List<ChatRecord>();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = includeArchived
            ? "SELECT Id, Title, Preview, CreatedAt, UpdatedAt, IsArchived FROM Chats ORDER BY UpdatedAt DESC;"
            : "SELECT Id, Title, Preview, CreatedAt, UpdatedAt, IsArchived FROM Chats WHERE IsArchived = 0 ORDER BY UpdatedAt DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ChatRecord(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                DateTime.Parse(reader.GetString(3)),
                DateTime.Parse(reader.GetString(4)),
                reader.GetInt32(5) != 0,
                reader.GetString(2)));
        }

        return result;
    }

    public void UpsertChat(Guid id, string title, string preview, bool isArchived)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Chats (Id, Title, Preview, CreatedAt, UpdatedAt, IsArchived)
            VALUES ($id, $title, $preview, $createdAt, $updatedAt, $archived)
            ON CONFLICT(Id) DO UPDATE SET
                Title = $title,
                Preview = $preview,
                UpdatedAt = $updatedAt,
                IsArchived = $archived;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$preview", preview);
        command.Parameters.AddWithValue("$createdAt", now);
        command.Parameters.AddWithValue("$updatedAt", now);
        command.Parameters.AddWithValue("$archived", isArchived ? 1 : 0);
        command.ExecuteNonQuery();
    }
public void RenameChat(Guid id, string title)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Chats SET Title = $title, UpdatedAt = $updatedAt WHERE Id = $id;";
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$id", id.ToString());
        command.ExecuteNonQuery();
    }

    public void SetChatArchived(Guid id, bool archived)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Chats SET IsArchived = $archived, UpdatedAt = $updatedAt WHERE Id = $id;";
        command.Parameters.AddWithValue("$archived", archived ? 1 : 0);
        command.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$id", id.ToString());
        command.ExecuteNonQuery();
    }

    public void DeleteChatRecord(Guid id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Messages WHERE ChatId = $id; DELETE FROM Chats WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.ExecuteNonQuery();
    }

    // ---------------------------------------------------------- Messages

    public List<MessageRecord> GetMessages(Guid chatId)
    {
        var result = new List<MessageRecord>();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ChatId, Role, Content, SentAt, AttachmentsJson FROM Messages WHERE ChatId = $chatId ORDER BY SentAt ASC;";
        command.Parameters.AddWithValue("$chatId", chatId.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new MessageRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4)),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return result;
    }

    public void AddMessage(Guid chatId, string role, string content, DateTime sentAt, string? attachmentsJson = null)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Messages (Id, ChatId, Role, Content, SentAt, AttachmentsJson)
            VALUES ($id, $chatId, $role, $content, $sentAt, $attachments);
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$chatId", chatId.ToString());
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$sentAt", sentAt.ToString("O"));
command.Parameters.AddWithValue("$attachments", attachmentsJson ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    // ---------------------------------------------------------- Memories

    public List<MemoryRecord> GetMemories()
    {
        var result = new List<MemoryRecord>();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Content, CreatedAt, UpdatedAt FROM Memories ORDER BY UpdatedAt DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new MemoryRecord(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                DateTime.Parse(reader.GetString(3)),
                DateTime.Parse(reader.GetString(4))));
        }

        return result;
    }

    public void SaveMemory(Guid id, string title, string content)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Memories (Id, Title, Content, CreatedAt, UpdatedAt)
            VALUES ($id, $title, $content, $createdAt, $updatedAt)
            ON CONFLICT(Id) DO UPDATE SET
                Title = $title,
                Content = $content,
                UpdatedAt = $updatedAt;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$createdAt", now);
        command.Parameters.AddWithValue("$updatedAt", now);
        command.ExecuteNonQuery();
    }

    public void DeleteMemory(Guid id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Memories WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.ExecuteNonQuery();
    }

    // ------------------------------------------------------------ Helpers

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }
}
