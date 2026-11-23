using Microsoft.Web.WebView2.Core;

namespace MiniGameBrowser.Core;

public static class WebViewSecurity
{
    private static readonly HashSet<string> BlockedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "javascript", "vbscript", "data", "file", "filesystem", "view-source", "chrome", "edge", "devtools",
        "ms-settings", "ms-appinstaller", "search-ms", "shell", "res", "jar", "ftp", "wss"
    };

    public static bool IsAllowedNavigation(string? raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return false;
        return !BlockedSchemes.Contains(uri.Scheme) &&
               (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }

    public static void Attach(CoreWebView2 core)
    {
        core.NavigationStarting += (_, e) => { if (!IsAllowedNavigation(e.Uri)) e.Cancel = true; };
        core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
        core.Settings.AreHostObjectsAllowed = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.IsWebMessageEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreDevToolsEnabled = false;
    }
}
