using SnippetMasterWPF.ViewModels.Windows;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Views.Pages;

public partial class ClipboardHistoryPage : INavigableView<ClipboardHistoryWindowViewModel>
{
    public ClipboardHistoryWindowViewModel ViewModel { get; }

    public ClipboardHistoryPage(ClipboardHistoryWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}