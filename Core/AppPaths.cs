namespace MiniGameBrowser.Core;

public static class AppPaths
{
    public static string Root { get; private set; } = "";
    public static string ProfilesRoot => Path.Combine(Root, "Profiles");
    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string FavoritesFile => Path.Combine(Root, "favorites.json");
    public static string WebViewDataFolder => Path.Combine(Root, "WebView2");
    public static string GetGroupWebViewDataFolder(int group) => Path.Combine(ProfilesRoot, $"Group-{group}");

    public static void Initialize()
    {
        Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiniGameBrowser-Rebuild");
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(ProfilesRoot);
        Directory.CreateDirectory(WebViewDataFolder);
    }
}
