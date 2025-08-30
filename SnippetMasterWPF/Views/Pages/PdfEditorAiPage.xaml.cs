using SnippetMasterWPF.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Views.Pages
{
    public partial class PdfEditorAiPage : INavigableView<PdfEditorAiViewModel>
    {
        public PdfEditorAiViewModel ViewModel { get; }

        public PdfEditorAiPage(PdfEditorAiViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}