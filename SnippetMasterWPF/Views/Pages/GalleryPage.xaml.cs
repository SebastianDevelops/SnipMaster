using SnippetMasterWPF.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Views.Pages;

public partial class GalleryPage : INavigableView<GalleryViewModel>
{
    public GalleryViewModel ViewModel { get; }

    public GalleryPage(GalleryViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}