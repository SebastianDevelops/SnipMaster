using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;

namespace SnippetMasterWPF.Services;

public class NotificationService : INotificationService
{
    private readonly ISnackbarService _snackbarService;

    public NotificationService(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
    }

    public void ShowNotification(string title, string message)
    {
        var appearance = GetAppearanceForMessage(message);
        var icon = GetIconForMessage(message);
        
        _snackbarService.Show(
            title,
            message,
            appearance,
            icon,
            TimeSpan.FromSeconds(4)
        );
    }

    public void ShowSuccess(string title, string message)
    {
        _snackbarService.Show(
            title,
            message,
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.Checkmark24),
            TimeSpan.FromSeconds(4)
        );
    }

    public void ShowError(string title, string message)
    {
        _snackbarService.Show(
            title,
            message,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowWarning(string title, string message)
    {
        _snackbarService.Show(
            title,
            message,
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24),
            TimeSpan.FromSeconds(4)
        );
    }

    public void ShowInfo(string title, string message)
    {
        _snackbarService.Show(
            title,
            message,
            ControlAppearance.Primary,
            new SymbolIcon(SymbolRegular.Info24),
            TimeSpan.FromSeconds(3)
        );
    }

    private static ControlAppearance GetAppearanceForMessage(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        
        if (lowerMessage.Contains("error") || lowerMessage.Contains("failed") || lowerMessage.Contains("unable"))
            return ControlAppearance.Danger;
        
        if (lowerMessage.Contains("warning") || lowerMessage.Contains("caution"))
            return ControlAppearance.Caution;
        
        if (lowerMessage.Contains("success") || lowerMessage.Contains("completed") || lowerMessage.Contains("saved") || lowerMessage.Contains("copied"))
            return ControlAppearance.Success;
        
        return ControlAppearance.Primary;
    }

    private static SymbolIcon? GetIconForMessage(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        
        if (lowerMessage.Contains("copied") || lowerMessage.Contains("clipboard"))
            return new SymbolIcon(SymbolRegular.Copy24);
        
        if (lowerMessage.Contains("saved") || lowerMessage.Contains("screenshot"))
            return new SymbolIcon(SymbolRegular.Save24);
        
        if (lowerMessage.Contains("error") || lowerMessage.Contains("failed"))
            return new SymbolIcon(SymbolRegular.ErrorCircle24);
        
        if (lowerMessage.Contains("warning"))
            return new SymbolIcon(SymbolRegular.Warning24);
        
        if (lowerMessage.Contains("success") || lowerMessage.Contains("completed"))
            return new SymbolIcon(SymbolRegular.Checkmark24);
        
        return new SymbolIcon(SymbolRegular.Info24);
    }
}