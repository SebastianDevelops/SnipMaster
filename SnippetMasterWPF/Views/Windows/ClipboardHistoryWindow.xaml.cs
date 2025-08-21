using SnippetMasterWPF.ViewModels.Windows;
using Wpf.Ui.Controls;
using System.Windows.Input;

namespace SnippetMasterWPF.Views.Windows;

public partial class ClipboardHistoryWindow : FluentWindow
{
    public ClipboardHistoryWindowViewModel ViewModel { get; }

    public ClipboardHistoryWindow(ClipboardHistoryWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}