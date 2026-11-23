using System.Text.Json;
using System.Text.Encodings.Web;
using MiniGameBrowser.Models;

namespace MiniGameBrowser.Core;

public sealed class JsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AppState Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile)) return new AppState();
            var json = File.ReadAllText(AppPaths.SettingsFile);
            var state = JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
            // Favorites are also stored in their own file. Load it so that the
            // separately persisted list is not silently lost on the next save.
            if (File.Exists(AppPaths.FavoritesFile))
            {
                try
                {
                    state.Favorites = JsonSerializer.Deserialize<List<FavoriteItem>>(
                        File.ReadAllText(AppPaths.FavoritesFile), Options) ?? state.Favorites;
                }
                catch (JsonException)
                {
                    TryBackup(AppPaths.FavoritesFile);
                }
            }
            state.Settings ??= new AppSettings();
            state.Settings.EnsureThreeGroupsSevenSlots();
            state.Favorites ??= [];
            state.Windows ??= [];
            return state;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            TryBackup(AppPaths.SettingsFile);
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        state.SchemaVersion = 2;
        AtomicWrite(AppPaths.SettingsFile, JsonSerializer.Serialize(state, Options));
        AtomicWrite(AppPaths.FavoritesFile, JsonSerializer.Serialize(state.Favorites, Options));
    }

    private static void AtomicWrite(string path, string content)
    {
        var temp = path + ".tmp";
        var backup = path + ".bak";
        File.WriteAllText(temp, content);
        try
        {
            if (File.Exists(path))
            {
                // File.Replace is atomic on Windows; remove an older backup first
                // to avoid platform-specific replacement failures.
                if (File.Exists(backup)) File.Delete(backup);
                File.Replace(temp, path, backup, true);
            }
            else
            {
                File.Move(temp, path);
            }
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static void TryBackup(string path)
    {
        try { if (File.Exists(path)) File.Copy(path, path + ".bad", true); } catch { }
    }
}
