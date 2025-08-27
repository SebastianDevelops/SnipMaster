using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Web.WebView2.Wpf;
using SnippetMasterWPF.Models.Editor;
using Wpf.Ui.Appearance;

namespace SnippetMasterWPF.Infrastructure.Editor;

public class EditorController
{
    private const string EditorContainerSelector = "#root";

    private const string EditorObject = "wpfUiMonacoEditor";

    private readonly WebView2 _webView;

    public EditorController(WebView2 webView)
    {
        _webView = webView;
    }

    public async Task CreateAsync()
    {
        _ = await _webView.ExecuteScriptAsync(
            $$"""
            const {{EditorObject}} = monaco.editor.create(document.querySelector('{{EditorContainerSelector}}'));
            window.onresize = () => {{{EditorObject}}.layout();}
            """
        );
    }

    public async Task SetThemeAsync(ApplicationTheme appApplicationTheme)
    {
        // TODO: Parse theme from object
        const string uiThemeName = "wpf-ui-app-theme";
        var baseMonacoTheme = appApplicationTheme == ApplicationTheme.Light ? "vs" : "vs-dark";

        _ = await _webView.ExecuteScriptAsync(
            $$$"""
            monaco.editor.defineTheme('{{{uiThemeName}}}', {
                base: '{{{baseMonacoTheme}}}',
                inherit: true,
                rules: [{ background: 'FFFFFF00' }],
                colors: {'editor.background': '#FFFFFF00','minimap.background': '#FFFFFF00',}});
            monaco.editor.setTheme('{{{uiThemeName}}}');
            """
        );
    }

    public async Task SetLanguageAsync(EditorLanguage monacoLanguage)
    {
        var languageId = monacoLanguage switch
        {
            EditorLanguage.Csharp => "csharp",
            EditorLanguage.ObjectiveC => "objective-c",
            _ => monacoLanguage.ToString().ToLower()
        };

        _ = await _webView.ExecuteScriptAsync(
            "monaco.editor.setModelLanguage(" + EditorObject + $".getModel(), \"{languageId}\");"
        );
    }

    public async Task SetContentAsync(string contents)
    {
        var literalContents = SymbolDisplay.FormatLiteral(contents, false);

        _ = await _webView.ExecuteScriptAsync(EditorObject + $".setValue(\"{literalContents}\");");
    }

    public async Task<string> GetContentAsync()
    {
        var result = await _webView.ExecuteScriptAsync(EditorObject + ".getValue();");
        if (string.IsNullOrEmpty(result)) return string.Empty;
        
        // Parse JSON string to properly handle escapes
        try
        {
            var content = System.Text.Json.JsonSerializer.Deserialize<string>(result);
            return content ?? string.Empty;
        }
        catch
        {
            // Fallback: manual unescaping
            var content = result.Trim('"');
            content = System.Text.RegularExpressions.Regex.Unescape(content);
            return content;
        }
    }

    public void DispatchScript(string script)
    {
        if (_webView == null)
        {
            return;
        }

        _ = Application.Current.Dispatcher.InvokeAsync(async () => await _webView!.ExecuteScriptAsync(script)
        );
    }
}