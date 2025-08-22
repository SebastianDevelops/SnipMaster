using SnippetMasterWPF.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Views.Pages
{
    public partial class FileConversionPage : Page, INavigableView<FileConversionViewModel>
    {
        public FileConversionViewModel ViewModel { get; }

        public FileConversionPage(FileConversionViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}