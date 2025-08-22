using CommunityToolkit.Mvvm.ComponentModel;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class PdfEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        [ObservableProperty]
        private bool _isPdfLoaded = false;
    }
}