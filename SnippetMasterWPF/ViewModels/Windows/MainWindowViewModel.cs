using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "SnippetMaster";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Text Snipper",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ScreenCut20 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "Quick Text Actions",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Flash24 },
                TargetPageType = typeof(Views.Pages.QuickTextActionsPage)
            },
            new NavigationViewItem()
            {
                Content = "Clipboard History (Ctrl+Shift+V)",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardTextLtr24 },
                TargetPageType = typeof(Views.Pages.ClipboardHistoryPage)
            },
            new NavigationViewItem()
            {
                Content = "Document Compression",
                Icon = new SymbolIcon { Symbol = SymbolRegular.FolderZip24 },
                TargetPageType = typeof(Views.Pages.DocumentCompressionPage)
            },
            new NavigationViewItem()
            {
                Content = "Image/Video Compression",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ImageMultiple24 },
                TargetPageType = typeof(Views.Pages.MediaCompressionPage)
            },
            new NavigationViewItem()
            {
                Content = "Text Comparison",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentLandscapeSplitHint24 },
                TargetPageType = typeof(Views.Pages.DataPage)
            },
            new NavigationViewItem()
            {
                Content = "File Conversion",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowSwap24 },
                TargetPageType = typeof(Views.Pages.FileConversionPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}
