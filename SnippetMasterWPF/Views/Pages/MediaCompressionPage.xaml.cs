using SnippetMasterWPF.ViewModels.Pages;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Views.Pages;

public partial class MediaCompressionPage : INavigableView<MediaCompressionViewModel>
{
    public MediaCompressionViewModel ViewModel { get; }

    public MediaCompressionPage(MediaCompressionViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private void DropArea_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ViewModel.ProcessDroppedFiles(files);
        }
    }

    private void DropArea_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
}