using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniGameBrowser.Models;

public sealed class AppState
{
    public int SchemaVersion { get; set; } = 2;
    public AppSettings Settings { get; set; } = new();
    public List<FavoriteItem> Favorites { get; set; } = [];
    public List<WindowPlacement> Windows { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? UnknownFields { get; set; }
}

public sealed class AppSettings
{
    public string StartUrl { get; set; } = "https://ro-idle.app/";
    public string SelectedUrl { get; set; } = "https://ro-idle.app/";
    // Compatibility field from the first reconstruction.
    public bool OpenFifthWindow { get; set; }
    public bool AskBeforeDownload { get; set; } = true;
    public bool BlockPopups { get; set; }
    public bool DarkMode { get; set; } = true;
    public string CharacterNameText { get; set; } = "";
    public int BatchWindowLimit { get; set; } = 6;
    // Keep the old four-window default; slots 5 and 6 are opt-in.
    public List<SlotInfo> Slots { get; set; } = CreateDefaultSlots();
    [JsonExtensionData] public Dictionary<string, JsonElement>? UnknownFields { get; set; }

    private static List<SlotInfo> CreateDefaultSlots() =>
    [
        new() { Group = 1, Number = 1, Enabled = true },
        new() { Group = 1, Number = 2, Enabled = true },
        new() { Group = 1, Number = 3, Enabled = true },
        new() { Group = 1, Number = 4, Enabled = true },
        new() { Group = 1, Number = 5, Enabled = false },
        new() { Group = 1, Number = 6, Enabled = false }
    ];
}

public sealed class FavoriteItem
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public sealed class WindowPlacement
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1100;
    public int Height { get; set; } = 760;
    public bool Maximized { get; set; }
    public string? SelectedUrl { get; set; }
}

public sealed class SlotInfo
{
    public int Group { get; set; } = 1;
    public int Number { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool IsStallSlot { get; set; }
    public int StallSlotNumber { get; set; }
    public string Url { get; set; } = "https://ro-idle.app/";
    public string SlotId => $"{Group}:{Number}";
}
