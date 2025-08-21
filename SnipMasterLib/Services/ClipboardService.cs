using Microsoft.Data.Sqlite;
using SnipMasterLib.Models;
using Dapper;
using TextCopy;
using System.IO;

namespace SnipMasterLib.Services;

public class ClipboardService : IClipboardService, IDisposable
{
    private readonly string _connectionString;
    private System.Threading.Timer? _clipboardTimer;
    private string _lastClipboardContent = string.Empty;

    public ClipboardService()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnippetMaster", "clipboard.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS ClipboardEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Content TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL,
                ContentType TEXT NOT NULL DEFAULT 'text'
            )");
    }

    public void StartMonitoring()
    {
        _clipboardTimer = new System.Threading.Timer(CheckClipboard, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public void StopMonitoring()
    {
        _clipboardTimer?.Dispose();
        _clipboardTimer = null;
    }

    private async void CheckClipboard(object? state)
    {
        try
        {
            var content = TextCopy.ClipboardService.GetText();
            if (!string.IsNullOrEmpty(content) && content != _lastClipboardContent)
            {
                await SaveClipboardEntryAsync(content);
                _lastClipboardContent = content;
            }
        }
        catch
        {
            // Clipboard access failed - continue monitoring
        }
    }

    private async Task SaveClipboardEntryAsync(string content)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "INSERT INTO ClipboardEntries (Content, CreatedAt, ContentType) VALUES (@Content, @CreatedAt, @ContentType)",
            new { Content = content, CreatedAt = DateTime.UtcNow, ContentType = "text" });
    }

    public async Task<IEnumerable<ClipboardEntry>> GetHistoryAsync(int limit = 50)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync<ClipboardEntry>(
            "SELECT * FROM ClipboardEntries ORDER BY CreatedAt DESC LIMIT @Limit",
            new { Limit = limit });
    }

    public async Task<ClipboardEntry?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<ClipboardEntry>(
            "SELECT * FROM ClipboardEntries WHERE Id = @Id", new { Id = id });
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM ClipboardEntries WHERE Id = @Id", new { Id = id });
    }

    public async Task ClearHistoryAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM ClipboardEntries");
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}