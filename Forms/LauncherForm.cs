using MiniGameBrowser.Core;
using MiniGameBrowser.Models;

namespace MiniGameBrowser.Forms;

/// <summary>Original-style launcher: A/B/C groups, seven visible slots per group, optional merchant batch slot.</summary>
public sealed class LauncherForm : Form
{
    private readonly AppState _state;
    private readonly JsonStore _store;
    private readonly Button[,] _slotButtons = new Button[3, 7];
    private readonly CheckBox[] _merchantChecks = new CheckBox[3];
    private readonly Label _countLabel = new() { AutoSize = true, ForeColor = Color.SteelBlue, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Dictionary<string, Form> _openForms = new();
    private static readonly string[] GroupNames = ["A", "B", "C"];

    public LauncherForm() : this(LoadInitialState()) { }
    private LauncherForm((AppState State, JsonStore Store) initial) : this(initial.State, initial.Store) { }
    private static (AppState State, JsonStore Store) LoadInitialState()
    {
        var store = new JsonStore();
        return (store.Load(), store);
    }

    public LauncherForm(AppState state, JsonStore store)
    {
        _state = state;
        _store = store;
        _state.Settings.EnsureThreeGroupsSevenSlots();
        Text = "極簡遊戲瀏覽器 v0.4.2（分享版）";
        ClientSize = new Size(584, 379);
        MinimumSize = new Size(584, 379);
        MaximumSize = new Size(584, 379);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft JhengHei UI", 9F);
        BackColor = SystemColors.Control;
        ForeColor = SystemColors.ControlText;
        BuildUi();
        RefreshButtons();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 7, 8, 5),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = SystemColors.Control
        };
        for (int i = 0; i < 3; i++) root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        for (int group = 1; group <= 3; group++)
            root.Controls.Add(BuildGroupRow(group), 0, group - 1);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 2, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var all = MakeButton("全部開啟", 132, 30);
        all.Click += async (_, _) => await OpenAllAsync();
        footer.Controls.Add(all, 0, 0);
        _countLabel.Dock = DockStyle.Fill;
        footer.Controls.Add(_countLabel, 1, 0);

        var isolation = new Label
        {
            Text = "同組視窗共用登入資料；A／B／C 三組各自獨立。",
            Dock = DockStyle.Fill,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft
        };
        footer.Controls.Add(isolation, 0, 1);
        footer.SetColumnSpan(isolation, 2);

        var merchantNote = new Label
        {
            Text = "※ 第七個（A7、B7、C7）為商人擺攤專用；勾選後會隨該組批次開啟。",
            Dock = DockStyle.Fill,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.TopLeft
        };
        footer.Controls.Add(merchantNote, 0, 2);
        footer.SetColumnSpan(merchantNote, 2);
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);
    }

    private Control BuildGroupRow(int group)
    {
        int index = group - 1;
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 1, 0, 1),
            Margin = new Padding(0)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var merchant = new CheckBox
        {
            Text = $"{GroupNames[index]} 組開啟第 7 個商人視窗",
            AutoSize = true,
            Margin = new Padding(2, 2, 0, 0),
            Checked = FindSlot(group, 7).Enabled
        };
        merchant.CheckedChanged += (_, _) =>
        {
            FindSlot(group, 7).Enabled = merchant.Checked;
            SaveSettings();
            UpdateCount();
        };
        _merchantChecks[index] = merchant;
        outer.Controls.Add(merchant, 0, 0);

        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        row.Controls.Add(new Label
        {
            Text = GroupNames[index],
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = false,
            Margin = new Padding(0),
            Font = new Font("Microsoft JhengHei UI", 9F)
        }, 0, 0);

        var slots = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
            AutoScroll = false
        };
        for (int number = 1; number <= 7; number++)
        {
            int slotNumber = number;
            var button = new SlotButton { Width = 61, Height = 50, Margin = new Padding(1), SlotTitle = $"{GroupNames[index]}{number}" };
            button.Click += (_, _) => OpenSingle(group, slotNumber);
            var menu = new ContextMenuStrip();
            menu.Items.Add("編輯帳號名稱…", null, (_, _) => RenameSlot(group, slotNumber));
            menu.Items.Add("清除自訂名稱", null, (_, _) => { FindSlot(group, slotNumber).DisplayName = ""; SaveSettings(); RefreshButtons(); });
            button.ContextMenuStrip = menu;
            _slotButtons[index, number - 1] = button;
            slots.Controls.Add(button);
        }
        row.Controls.Add(slots, 1, 0);

        var openGroup = MakeButton($"開啟 {GroupNames[index]} 組", 74, 32);
        openGroup.Click += async (_, _) => await OpenGroupAsync(group);
        row.Controls.Add(openGroup, 2, 0);
        outer.Controls.Add(row, 0, 1);
        return outer;
    }

    private SlotInfo FindSlot(int group, int number) => _state.Settings.Slots.First(x => x.Group == group && x.Number == number);

    private void OpenSingle(int group, int number)
    {
        try
        {
            PersistSettings();
            var slot = FindSlot(group, number);
            var key = slot.SlotId;
            if (_openForms.TryGetValue(key, out var existing) && !existing.IsDisposed)
            {
                existing.WindowState = FormWindowState.Normal;
                existing.Activate();
                return;
            }
            var url = string.IsNullOrWhiteSpace(slot.Url) ? _state.Settings.StartUrl : slot.Url;
            var browser = new BrowserForm(_state, _store, url, group, number, name => UpdateCharacterName(group, number, name));
            _openForms[key] = browser;
            browser.FormClosed += (_, _) =>
            {
                _openForms.Remove(key);
                RefreshButtons();
            };
            browser.Show();
            RefreshButtons();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "開啟視窗失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateCharacterName(int group, int number, string name)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateCharacterName(group, number, name)));
            return;
        }
        var slot = FindSlot(group, number);
        slot.CharacterName = name;
        SaveSettings();
        RefreshButtons();
    }

    private async Task OpenGroupAsync(int group)
    {
        PersistSettings();
        for (int number = 1; number <= 6; number++)
        {
            OpenSingle(group, number);
            await Task.Delay(160);
        }
        if (FindSlot(group, 7).Enabled)
        {
            OpenSingle(group, 7);
            await Task.Delay(160);
        }
        UpdateCount();
    }

    private async Task OpenAllAsync()
    {
        PersistSettings();
        for (int group = 1; group <= 3; group++)
        {
            for (int number = 1; number <= 6; number++)
            {
                OpenSingle(group, number);
                await Task.Delay(160);
            }
            if (FindSlot(group, 7).Enabled)
            {
                OpenSingle(group, 7);
                await Task.Delay(160);
            }
        }
        UpdateCount();
    }

    private void RenameSlot(int group, int number)
    {
        var slot = FindSlot(group, number);
        using var dialog = new Form
        {
            Text = $"編輯 {GroupNames[group - 1]}{number} 帳號名稱",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(300, 105),
            MaximizeBox = false,
            MinimizeBox = false
        };
        var input = new TextBox { Left = 12, Top = 12, Width = 276, Text = slot.DisplayName };
        var ok = new Button { Text = "確定", Left = 132, Top = 55, Width = 72, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", Left = 212, Top = 55, Width = 72, DialogResult = DialogResult.Cancel };
        dialog.Controls.AddRange([input, ok, cancel]);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            slot.DisplayName = input.Text.Trim();
            SaveSettings();
            RefreshButtons();
        }
    }

    private void RefreshButtons()
    {
        for (int group = 1; group <= 3; group++)
        for (int number = 1; number <= 7; number++)
        {
            var slot = FindSlot(group, number);
            bool opened = _openForms.TryGetValue(slot.SlotId, out var form) && !form.IsDisposed;
            var detected = opened ? slot.CharacterName : "";
            var label = !string.IsNullOrWhiteSpace(detected)
                ? detected
                : !string.IsNullOrWhiteSpace(slot.DisplayName) ? slot.DisplayName
                : opened ? "已開啟" : "未開啟";
            if (_slotButtons[group - 1, number - 1] is SlotButton button)
            {
                button.StatusText = label;
                button.CharacterDetected = !string.IsNullOrWhiteSpace(detected);
                button.Invalidate();
            }
        }
        UpdateCount();
    }

    private void UpdateCount()
    {
        int total = 18 + _state.Settings.Slots.Count(x => x.Number == 7 && x.Enabled);
        int opened = _openForms.Count(x => !x.Value.IsDisposed);
        _countLabel.Text = $"全部開啟（{total} 個視窗）　目前已開啟 {opened} 個";
    }

    private void PersistSettings()
    {
        _state.Settings.BatchWindowLimit = 21;
        SaveSettings();
    }

    private void SaveSettings()
    {
        _state.Settings.EnsureThreeGroupsSevenSlots();
        _store.Save(_state);
    }

    private static Button MakeButton(string text, int width, int height) => new()
    {
        Text = text,
        Width = width,
        Height = height,
        FlatStyle = FlatStyle.Standard,
        BackColor = SystemColors.Control,
        ForeColor = SystemColors.ControlText,
        Font = new Font("Microsoft JhengHei UI", 9F),
        TextAlign = ContentAlignment.MiddleCenter,
        UseVisualStyleBackColor = true
    };

    private sealed class SlotButton : Button
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string SlotTitle { get; set; } = "";

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string StatusText { get; set; } = "未開啟";

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool CharacterDetected { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var state = !Enabled ? ButtonState.Inactive : Capture ? ButtonState.Pushed : ButtonState.Normal;
            ControlPaint.DrawButton(e.Graphics, ClientRectangle, state);
            var titleRect = new Rectangle(2, 5, ClientSize.Width - 4, 18);
            var statusRect = new Rectangle(2, 24, ClientSize.Width - 4, ClientSize.Height - 26);
            TextRenderer.DrawText(e.Graphics, SlotTitle, Font, titleRect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            var statusFont = CharacterDetected ? new Font(Font, FontStyle.Bold) : Font;
            try
            {
                TextRenderer.DrawText(e.Graphics, StatusText, statusFont, statusRect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
            }
            finally
            {
                if (CharacterDetected) statusFont.Dispose();
            }
        }
    }
}
