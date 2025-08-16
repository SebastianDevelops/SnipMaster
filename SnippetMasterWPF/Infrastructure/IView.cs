using Microsoft.Web.WebView2.Wpf;

namespace SnippetMasterWPF.Infrastructure
{
    public interface IView
    {
        WebView2 WebView { get; }
    }
}