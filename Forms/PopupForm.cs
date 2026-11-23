using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MiniGameBrowser.Core;

namespace MiniGameBrowser.Forms;

public sealed class PopupForm : Form
{
    private readonly string _url;
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    public PopupForm(string url)
    {
        _url = url; Text = "MiniGameBrowser — 彈出視窗"; Width = 900; Height = 650;
        Controls.Add(_web);
        Shown += async (_, _) => await InitializeAsync();
    }
    private async Task InitializeAsync()
    {
        if (!WebViewSecurity.IsAllowedNavigation(_url)) { Close(); return; }
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(AppPaths.WebViewDataFolder, "Popup"));
            await _web.EnsureCoreWebView2Async(env);
            WebViewSecurity.Attach(_web.CoreWebView2);
            _web.CoreWebView2.NewWindowRequested += (_, e) => { e.Handled = true; if (WebViewSecurity.IsAllowedNavigation(e.Uri)) new PopupForm(e.Uri).Show(this); };
            _web.CoreWebView2.Navigate(_url);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "彈出視窗初始化失敗"); Close(); }
    }
}
