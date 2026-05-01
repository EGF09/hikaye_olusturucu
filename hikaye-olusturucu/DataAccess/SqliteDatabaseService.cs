using System.Data.SQLite;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Models;
using hikaye_olusturucu.Core.Interfaces;

namespace hikaye_olusturucu.DataAccess;

public class SqliteDatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public SqliteDatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            CREATE TABLE IF NOT EXISTS Stories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Prompt TEXT NOT NULL,
                Content TEXT NOT NULL,
                ImagePaths TEXT,
                AudioPath TEXT,
                VideoPath TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        using var command = new SQLiteCommand(query, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveStoryAsync(Story story)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            INSERT INTO Stories (Prompt, Content, ImagePaths, AudioPath, VideoPath, CreatedAt)
            VALUES (@Prompt, @Content, @ImagePaths, @AudioPath, @VideoPath, @CreatedAt)";

        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@Prompt", story.Prompt);
        command.Parameters.AddWithValue("@Content", story.Content);
        command.Parameters.AddWithValue("@ImagePaths", string.Join(";", story.ImagePaths));
        command.Parameters.AddWithValue("@AudioPath", story.AudioPath);
        command.Parameters.AddWithValue("@VideoPath", story.VideoPath);
        command.Parameters.AddWithValue("@CreatedAt", story.CreatedAt);

        await command.ExecuteNonQueryAsync();
    }
}