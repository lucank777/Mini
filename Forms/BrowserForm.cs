using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MiniGameBrowser.Core;
using MiniGameBrowser.Models;

namespace MiniGameBrowser.Forms;

public sealed class BrowserForm : Form
{
    private readonly AppState _state;
    private readonly JsonStore _store;
    private readonly TextBox _address = new();
    private readonly ToolStripButton _back = new("‹");
    private readonly ToolStripButton _forward = new("›");
    private readonly ToolStripButton _reload = new("↻");
    private readonly ToolStripButton _home = new("⌂");
    private readonly ToolStripButton _star = new("☆");
    private readonly ToolStripButton _favorites = new("收藏");
    private readonly ToolStripButton _launcher = new("啟動器");
    private readonly ToolStrip _toolbar = new();
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = false, Height = 22, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
    private bool _ready;
    private readonly int _group;
    private readonly Action<string>? _onCharacterName;
    private readonly System.Windows.Forms.Timer _characterTimer = new() { Interval = 1500 };
    private int _characterChecks;

    public BrowserForm(AppState state, JsonStore store, string? startUrl = null, int group = 1, int slotNumber = 1, Action<string>? onCharacterName = null)
    {
        _group = Math.Clamp(group, 1, 3);
        _onCharacterName = onCharacterName;
        _state = state; _store = store;
        Text = $"極簡遊戲瀏覽器 - {_group} 組"; Width = 1180; Height = 800; MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(25, 27, 31); ForeColor = Color.White;
        BuildToolbar();
        Controls.Add(_web); Controls.Add(_status); Controls.Add(_toolbar);
        _toolbar.Dock = DockStyle.Top;
        _address.Text = startUrl ?? _state.Settings.SelectedUrl;
        Shown += async (_, _) => await InitializeWebViewAsync();
        FormClosing += (_, _) => { _characterTimer.Stop(); _characterTimer.Dispose(); SaveWindowState(); };
        _characterTimer.Tick += async (_, _) => await RefreshCharacterNameAsync();
    }

    private void BuildToolbar()
    {
        _toolbar.GripStyle = ToolStripGripStyle.Hidden; _toolbar.Padding = new Padding(8, 6, 8, 6);
        _toolbar.BackColor = Color.FromArgb(34, 37, 43); _toolbar.ForeColor = Color.White;
        foreach (var b in new[] { _back, _forward, _reload, _home }) { b.DisplayStyle = ToolStripItemDisplayStyle.Text; b.Font = new Font("Segoe UI", 12); _toolbar.Items.Add(b); }
        _address.BorderStyle = BorderStyle.FixedSingle; _address.Width = 650; _address.Font = new Font("Segoe UI", 10);
        _toolbar.Items.Add(new ToolStripControlHost(_address) { AutoSize = false, Width = 650, Height = 28, Margin = new Padding(8, 1, 8, 1) });
        foreach (var b in new[] { _star, _favorites, _launcher }) _toolbar.Items.Add(b);
        _back.Click += (_, _) => { if (_ready && _web.CoreWebView2.CanGoBack) _web.CoreWebView2.GoBack(); };
        _forward.Click += (_, _) => { if (_ready && _web.CoreWebView2.CanGoForward) _web.CoreWebView2.GoForward(); };
        _reload.Click += (_, _) => { if (_ready) _web.CoreWebView2.Reload(); };
        _home.Click += (_, _) => Navigate(_state.Settings.StartUrl);
        _address.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { Navigate(_address.Text); e.SuppressKeyPress = true; } };
        _star.Click += (_, _) => AddCurrentToFavorites();
        _favorites.Click += (_, _) => new FavoritesForm(_state, _store, url => Navigate(url)).Show(this);
        _launcher.Click += (_, _) => new LauncherForm(_state, _store).Show();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, AppPaths.GetGroupWebViewDataFolder(_group));
            await _web.EnsureCoreWebView2Async(env);
            _ready = true;
            WebViewSecurity.Attach(_web.CoreWebView2);
            _web.CoreWebView2.SourceChanged += (_, _) => SyncAddressBar();
            _web.CoreWebView2.DocumentTitleChanged += (_, _) => UpdateAddressAndTitle();
            _web.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                _status.Text = e.IsSuccess ? "完成" : $"載入失敗：{e.WebErrorStatus}";
                _back.Enabled = _web.CoreWebView2.CanGoBack; _forward.Enabled = _web.CoreWebView2.CanGoForward;
                if (e.IsSuccess && IsTrustedCharacterPage())
                {
                    _characterChecks = 0;
                    _characterTimer.Stop();
                    _characterTimer.Start();
                    _ = RefreshCharacterNameAsync();
                }
                else _characterTimer.Stop();
            };
            _web.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (!_state.Settings.BlockPopups && WebViewSecurity.IsAllowedNavigation(e.Uri))
                    new PopupForm(e.Uri).Show(this);
            };
            _web.CoreWebView2.DownloadStarting += (_, e) =>
            {
                if (_state.Settings.AskBeforeDownload)
                {
                    var name = Path.GetFileName(e.ResultFilePath);
                    if (MessageBox.Show(this, $"是否允許下載？\n{name}", "下載確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        e.Cancel = true;
                }
            };
            _web.CoreWebView2.LaunchingExternalUriScheme += (_, e) =>
            {
                e.Cancel = true;
                _status.Text = "已阻止外部通訊協定：" + e.Uri;
            };
            Navigate(_address.Text);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(this, "找不到 Microsoft Edge WebView2 Runtime。請安裝 Evergreen Runtime。", "WebView2 Runtime", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _status.Text = "WebView2 Runtime 未安裝";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "瀏覽器初始化失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private bool IsTrustedCharacterPage()
    {
        if (!_ready || !Uri.TryCreate(_web.CoreWebView2.Source, UriKind.Absolute, out var uri))
            return false;
        return uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Equals("ro-idle.app", StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort;
    }

    private async Task RefreshCharacterNameAsync()
    {
        if (!_ready || IsDisposed || !IsTrustedCharacterPage())
        {
            _characterTimer.Stop();
            return;
        }
        if (++_characterChecks > 30)
        {
            _characterTimer.Stop();
            return;
        }

        const string script = @"(() => {
            const text = document.body && document.body.innerText ? document.body.innerText : '';
            const lines = text.split(/\\r?\\n/).map(x => x.trim()).filter(Boolean);
            const index = lines.findIndex(x => /Base\\s*Lv/i.test(x));
            if (index < 0) return '';
            const candidates = [lines[index - 1], lines[index + 1], lines[index + 2], lines[index + 3]];
            for (const value of candidates) {
                if (!value) continue;
                if (/Base\\s*Lv|Job\\s*Lv|Class|職業|等級|\\d+\\s*\\/?\\s*\\d*/i.test(value)) continue;
                if (value.length > 24) continue;
                return value;
            }
            return '';
        })();";

        try
        {
            var raw = await _web.ExecuteScriptAsync(script);
            var name = JsonSerializer.Deserialize<string>(raw)?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name)) return;
            _characterTimer.Stop();
            _onCharacterName?.Invoke(name);
        }
        catch
        {
            // The page may still be rendering; retry on the next timer tick.
        }
    }

    private void Navigate(string raw)
    {
        if (!UrlHelper.TryNormalize(raw, out var uri) || uri is null) { _status.Text = "網址或搜尋內容無效"; return; }
        _address.Text = uri.ToString();
        if (_ready) _web.CoreWebView2.Navigate(uri.ToString());
    }

    private void SyncAddressBar()
    {
        if (_ready && _web.CoreWebView2.Source is string src) _address.Text = src;
    }

    private void UpdateAddressAndTitle()
    {
        SyncAddressBar();
        if (_ready)
        {
            var title = _web.CoreWebView2.DocumentTitle;
            Text = string.IsNullOrWhiteSpace(title) ? "MiniGameBrowser" : title + " — MiniGameBrowser";
            _state.Settings.SelectedUrl = _web.CoreWebView2.Source;
            _store.Save(_state);
        }
    }

    private void AddCurrentToFavorites()
    {
        if (!_ready) return;
        var url = _web.CoreWebView2.Source;
        if (_state.Favorites.Any(x => x.Url.Equals(url, StringComparison.OrdinalIgnoreCase))) return;
        _state.Favorites.Add(new FavoriteItem { Url = url, Title = string.IsNullOrWhiteSpace(_web.CoreWebView2.DocumentTitle) ? url : _web.CoreWebView2.DocumentTitle });
        _store.Save(_state); _status.Text = "已加入收藏";
    }

    private void SaveWindowState()
    {
        if (_ready) _state.Settings.SelectedUrl = _web.CoreWebView2.Source;
        _store.Save(_state);
    }
}
