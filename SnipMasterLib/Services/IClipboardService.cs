using SnipMasterLib.Models;

namespace SnipMasterLib.Services;

public interface IClipboardService
{
    Task<IEnumerable<ClipboardEntry>> GetHistoryAsync(int limit = 50);
    Task<ClipboardEntry?> GetByIdAsync(int id);
    Task DeleteAsync(int id);
    Task ClearHistoryAsync();
    void StartMonitoring();
    void StopMonitoring();
}