using SnipMasterLib.Models;

namespace SnipMasterLib.Services;

public interface IClipboardService
{
    Task<IEnumerable<ClipboardEntry>> GetHistoryAsync(int limit = 50);
    Task<IEnumerable<ClipboardEntry>> SearchAsync(string searchTerm, int page = 0, int pageSize = 10);
    Task<ClipboardEntry?> GetByIdAsync(int id);
    Task DeleteAsync(int id);
    Task ClearHistoryAsync();
    Task TogglePinAsync(int id);
    void StartMonitoring();
    void StopMonitoring();
}