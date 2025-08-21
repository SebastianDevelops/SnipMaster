using SnippetMasterWPF.ViewModels.Pages;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Views.Pages;

public partial class QuickTextActionsPage : INavigableView<QuickTextActionsViewModel>
{
    public QuickTextActionsViewModel ViewModel { get; }

    public QuickTextActionsPage(QuickTextActionsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        };
        var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
        parent?.RaiseEvent(eventArg);
    }

    private System.Windows.Controls.ScrollViewer? FindScrollViewer(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is System.Windows.Controls.ScrollViewer scrollViewer)
                return scrollViewer;
            var result = FindScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
    }
}