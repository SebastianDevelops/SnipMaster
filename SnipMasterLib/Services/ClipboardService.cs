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
        
        // Create table if not exists
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS ClipboardEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Content TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL,
                ContentType TEXT NOT NULL DEFAULT 'text'
            )");
        
        // Add IsPinned column if it doesn't exist (migration)
        try
        {
            connection.Execute("ALTER TABLE ClipboardEntries ADD COLUMN IsPinned BOOLEAN NOT NULL DEFAULT 0");
        }
        catch
        {
            // Column already exists, ignore error
        }
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
        
        // Check if content already exists
        var existingEntry = await connection.QueryFirstOrDefaultAsync<ClipboardEntry>(
            "SELECT * FROM ClipboardEntries WHERE Content = @Content",
            new { Content = content });
        
        if (existingEntry != null)
        {
            // Update existing entry's timestamp to move it to top
            await connection.ExecuteAsync(
                "UPDATE ClipboardEntries SET CreatedAt = @CreatedAt WHERE Id = @Id",
                new { CreatedAt = DateTime.UtcNow, Id = existingEntry.Id });
        }
        else
        {
            // Insert new entry
            await connection.ExecuteAsync(
                "INSERT INTO ClipboardEntries (Content, CreatedAt, ContentType, IsPinned) VALUES (@Content, @CreatedAt, @ContentType, @IsPinned)",
                new { Content = content, CreatedAt = DateTime.UtcNow, ContentType = "text", IsPinned = false });
        }
        
        // Auto-cleanup: Keep only 30 most recent unpinned entries
        await connection.ExecuteAsync(@"
            DELETE FROM ClipboardEntries 
            WHERE IsPinned = 0 
            AND Id NOT IN (
                SELECT Id FROM ClipboardEntries 
                WHERE IsPinned = 0 
                ORDER BY CreatedAt DESC 
                LIMIT 30
            )");
    }

    public async Task<IEnumerable<ClipboardEntry>> GetHistoryAsync(int limit = 50)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync<ClipboardEntry>(
            "SELECT * FROM ClipboardEntries ORDER BY IsPinned DESC, CreatedAt DESC LIMIT @Limit",
            new { Limit = limit });
    }

    public async Task<IEnumerable<ClipboardEntry>> SearchAsync(string searchTerm, int page = 0, int pageSize = 10)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var offset = page * pageSize;
        return await connection.QueryAsync<ClipboardEntry>(
            "SELECT * FROM ClipboardEntries WHERE Content LIKE @SearchTerm ORDER BY IsPinned DESC, CreatedAt DESC LIMIT @PageSize OFFSET @Offset",
            new { SearchTerm = $"%{searchTerm}%", PageSize = pageSize, Offset = offset });
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
        await connection.ExecuteAsync("DELETE FROM ClipboardEntries WHERE IsPinned = 0");
    }

    public async Task TogglePinAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "UPDATE ClipboardEntries SET IsPinned = NOT IsPinned WHERE Id = @Id",
            new { Id = id });
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}