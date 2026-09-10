using Microsoft.Data.Sqlite;

namespace FROX.Data;

public sealed class FroxDbContext
{
    private readonly string _databasePath;

    public FroxDbContext(string? databasePath = null)
    {
        _databasePath = databasePath ?? Path.Combine(AppContext.BaseDirectory, "frox.db");
        EnsureCreated();
    }

    public string DatabasePath => _databasePath;

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? AppContext.BaseDirectory);

        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
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
            );";
        command.ExecuteNonQuery();
    }
}
