using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnipMasterLib.Models;
using SnipMasterLib.Services;
using SnippetMasterWPF.Services;
using System.Collections.ObjectModel;
using TextCopy;

namespace SnippetMasterWPF.ViewModels.Windows;

public partial class ClipboardHistoryWindowViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<ClipboardEntry> _clipboardEntries = new();

    [ObservableProperty]
    private string _searchText = string.Empty;



    public ClipboardHistoryWindowViewModel(IClipboardService clipboardService, INotificationService notificationService)
    {
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        LoadClipboardHistory();
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchEntries();
    }

    private async void LoadClipboardHistory()
    {
        var entries = await _clipboardService.GetHistoryAsync(30);
        ClipboardEntries.Clear();
        foreach (var entry in entries)
        {
            ClipboardEntries.Add(entry);
        }
    }

    private async void SearchEntries()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            LoadClipboardHistory();
            return;
        }

        var entries = await _clipboardService.SearchAsync(SearchText, 0, 50);
        ClipboardEntries.Clear();
        foreach (var entry in entries)
        {
            ClipboardEntries.Add(entry);
        }
    }

    [RelayCommand]
    private async Task CopyToClipboard(ClipboardEntry entry)
    {
        await TextCopy.ClipboardService.SetTextAsync(entry.Content);
        _notificationService.ShowNotification("Clipboard", "Copied to clipboard");
    }

    [RelayCommand]
    private async Task TogglePin(ClipboardEntry entry)
    {
        await _clipboardService.TogglePinAsync(entry.Id);
        
        // Get updated entry from database to ensure correct state
        var updatedEntry = await _clipboardService.GetByIdAsync(entry.Id);
        if (updatedEntry != null)
        {
            entry.IsPinned = updatedEntry.IsPinned;
            _notificationService.ShowNotification("Pin", entry.IsPinned ? "Pinned" : "Unpinned");
        }
    }

    [RelayCommand]
    private async Task DeleteEntry(ClipboardEntry entry)
    {
        await _clipboardService.DeleteAsync(entry.Id);
        ClipboardEntries.Remove(entry);
        _notificationService.ShowNotification("Deleted", "Clipboard entry removed");
    }


}