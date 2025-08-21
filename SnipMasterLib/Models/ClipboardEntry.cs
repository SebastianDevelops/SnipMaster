using System.ComponentModel;

namespace SnipMasterLib.Models;

public class ClipboardEntry : INotifyPropertyChanged
{
    private bool _isPinned = false;

    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ContentType { get; set; } = "text";
    
    public bool IsPinned 
    { 
        get => _isPinned;
        set
        {
            if (_isPinned != value)
            {
                _isPinned = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}