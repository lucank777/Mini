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
    public int BatchWindowLimit { get; set; } = 21;
    // Three groups, six regular slots plus one optional merchant/stall slot per group.
    public List<SlotInfo> Slots { get; set; } = CreateDefaultSlots();
    [JsonExtensionData] public Dictionary<string, JsonElement>? UnknownFields { get; set; }

    public static List<SlotInfo> CreateDefaultSlots()
    {
        var slots = new List<SlotInfo>();
        for (int group = 1; group <= 3; group++)
        {
            for (int number = 1; number <= 7; number++)
            {
                slots.Add(new SlotInfo
                {
                    Group = group,
                    Number = number,
                    Enabled = number <= 6,
                    IsStallSlot = number == 7,
                    StallSlotNumber = number == 7 ? 7 : 0,
                    DisplayName = "",
                    CharacterName = ""
                });
            }
        }
        return slots;
    }

    public void EnsureThreeGroupsSevenSlots()
    {
        Slots ??= [];
        for (int group = 1; group <= 3; group++)
        for (int number = 1; number <= 7; number++)
        {
            var existing = Slots.FirstOrDefault(x => x.Group == group && x.Number == number);
            if (existing is null)
            {
                Slots.Add(new SlotInfo
                {
                    Group = group,
                    Number = number,
                    Enabled = number <= 6,
                    IsStallSlot = number == 7,
                    StallSlotNumber = number == 7 ? 7 : 0,
                    DisplayName = "",
                    CharacterName = ""
                });
            }
            else if (number <= 6)
            {
                // All six regular account slots are always included in their group.
                existing.Enabled = true;
                existing.IsStallSlot = false;
                existing.StallSlotNumber = 0;
                if (existing.DisplayName is "預設無關" or "老鷹" or "封天" or "豐珠" or "魔丸")
                    existing.DisplayName = "";
            }
            else
            {
                existing.IsStallSlot = true;
                existing.StallSlotNumber = 7;
                // Clear legacy placeholder labels; slot status and detected character name are shown by the launcher.
                if (existing.DisplayName is "預設無關" or "老鷹" or "封天" or "豐珠" or "魔丸" or "商人擺攤")
                    existing.DisplayName = "";
            }
            existing.CharacterName ??= "";
        }
        Slots = Slots.Where(x => x.Group >= 1 && x.Group <= 3 && x.Number >= 1 && x.Number <= 7)
            .OrderBy(x => x.Group).ThenBy(x => x.Number).ToList();
    }
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
    public string DisplayName { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string Url { get; set; } = "https://ro-idle.app/";
    public string SlotId => $"{Group}:{Number}";
}
