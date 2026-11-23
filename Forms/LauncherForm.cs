using MiniGameBrowser.Core;
using MiniGameBrowser.Models;

namespace MiniGameBrowser.Forms;

public sealed class LauncherForm : Form
{
    private readonly AppState _state;
    private readonly JsonStore _store;
    private readonly TextBox _startUrl = new();
    private readonly DataGridView _slotGrid = new();
    private readonly CheckBox _blockPopups = new() { Text = "封鎖網站彈出視窗", AutoSize = true };
    private readonly CheckBox _askDownloads = new() { Text = "下載前詢問", AutoSize = true, Checked = true };

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
        Text = "MiniGameBrowser — 啟動器";
        Width = 900; Height = 650; MinimumSize = new Size(760, 520);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(25, 27, 31);
        ForeColor = Color.White;
        // Migrate the legacy fifth-window toggle into the explicit Slot list.
        if (_state.Settings.OpenFifthWindow)
        {
            var fifth = _state.Settings.Slots.FirstOrDefault(x => x.Group == 1 && x.Number == 5);
            if (fifth is not null) fifth.Enabled = true;
        }
        BuildUi();
        LoadSlots();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        var heading = new Label { Text = "MiniGameBrowser　群組 / Slot 啟動器", Font = new Font("Segoe UI", 18, FontStyle.Bold), Dock = DockStyle.Fill, ForeColor = Color.White };
        root.Controls.Add(heading, 0, 0);

        var urlPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        urlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        urlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        urlPanel.Controls.Add(new Label { Text = "預設網址", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Gainsboro }, 0, 0);
        _startUrl.Dock = DockStyle.Fill; _startUrl.Text = _state.Settings.StartUrl;
        urlPanel.Controls.Add(_startUrl, 1, 0);
        root.Controls.Add(urlPanel, 0, 1);

        ConfigureGrid();
        root.Controls.Add(_slotGrid, 0, 2);
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _blockPopups.Checked = _state.Settings.BlockPopups;
        _askDownloads.Checked = _state.Settings.AskBeforeDownload;
        options.Controls.Add(_blockPopups); options.Controls.Add(_askDownloads);
        options.Controls.Add(new Label { Text = "批次上限：6 視窗（預設維持 4 個啟用，第 5、6 個可自行開啟）", AutoSize = true, ForeColor = Color.LightSkyBlue, Padding = new Padding(14, 4, 0, 0) });
        root.Controls.Add(options, 0, 3);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var batch = MakeButton("依啟用 Slot 批次開啟", true); batch.Click += async (_, _) => await OpenManyAsync();
        var launch = MakeButton("開啟單一視窗", false); launch.Click += (_, _) => LaunchOne();
        var add = MakeButton("新增 Slot", false); add.Click += (_, _) => AddSlot();
        var remove = MakeButton("刪除選取 Slot", false); remove.Click += (_, _) => RemoveSelectedSlot();
        var favorites = MakeButton("收藏管理", false); favorites.Click += (_, _) => new FavoritesForm(_state, _store, url => new BrowserForm(_state, _store, url).Show()).Show(this);
        actions.Controls.Add(batch); actions.Controls.Add(launch); actions.Controls.Add(add); actions.Controls.Add(remove); actions.Controls.Add(favorites);
        root.Controls.Add(actions, 0, 4);
        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        _slotGrid.Dock = DockStyle.Fill;
        _slotGrid.BackgroundColor = Color.FromArgb(34, 37, 43);
        _slotGrid.ForeColor = Color.Black;
        _slotGrid.AllowUserToAddRows = false; _slotGrid.AllowUserToDeleteRows = false;
        _slotGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _slotGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _slotGrid.MultiSelect = false;
        _slotGrid.RowHeadersVisible = false;
        _slotGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "啟用", FillWeight = 45 });
        _slotGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Group", HeaderText = "群組", FillWeight = 55 });
        _slotGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Number", HeaderText = "Slot", FillWeight = 45 });
        _slotGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "此 Slot 網址", FillWeight = 220 });
        _slotGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsStallSlot", HeaderText = "擺攤 Slot", FillWeight = 65 });
        _slotGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "StallSlotNumber", HeaderText = "擺攤編號", FillWeight = 65 });
        _slotGrid.CurrentCellDirtyStateChanged += (_, _) => { if (_slotGrid.IsCurrentCellDirty) _slotGrid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        _slotGrid.DataError += (_, e) => { e.ThrowException = false; };
    }

    private void LoadSlots()
    {
        _slotGrid.Rows.Clear();
        foreach (var slot in _state.Settings.Slots.OrderBy(x => x.Group).ThenBy(x => x.Number))
            _slotGrid.Rows.Add(slot.Enabled, slot.Group, slot.Number, string.IsNullOrWhiteSpace(slot.Url) ? _state.Settings.StartUrl : slot.Url, slot.IsStallSlot, slot.StallSlotNumber);
    }

    private void CaptureSlots()
    {
        var slots = new List<SlotInfo>();
        foreach (DataGridViewRow row in _slotGrid.Rows)
        {
            if (row.IsNewRow) continue;
            if (!int.TryParse(Convert.ToString(row.Cells["Group"].Value), out int group) || group < 1)
                throw new InvalidOperationException("群組必須是大於 0 的整數。");
            if (!int.TryParse(Convert.ToString(row.Cells["Number"].Value), out int number) || number < 1)
                throw new InvalidOperationException("Slot 編號必須是大於 0 的整數。");
            var url = Convert.ToString(row.Cells["Url"].Value)?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(url)) url = _startUrl.Text.Trim();
            slots.Add(new SlotInfo
            {
                Group = group, Number = number,
                Enabled = Convert.ToBoolean(row.Cells["Enabled"].Value ?? false),
                Url = NormalizeUrl(url),
                IsStallSlot = Convert.ToBoolean(row.Cells["IsStallSlot"].Value ?? false),
                StallSlotNumber = int.TryParse(Convert.ToString(row.Cells["StallSlotNumber"].Value), out int stall) ? stall : 0
            });
        }
        var duplicate = slots.GroupBy(x => x.SlotId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"群組 {duplicate.First().Group} 的 Slot {duplicate.First().Number} 重複。");
        _state.Settings.Slots = slots;
    }

    private void Persist()
    {
        CaptureSlots();
        _state.Settings.StartUrl = NormalizeUrl(_startUrl.Text);
        _state.Settings.BlockPopups = _blockPopups.Checked;
        _state.Settings.AskBeforeDownload = _askDownloads.Checked;
        _state.Settings.BatchWindowLimit = 6;
        _state.Settings.OpenFifthWindow = _state.Settings.Slots.Any(x => x.Group == 1 && x.Number == 5 && x.Enabled);
        _store.Save(_state);
    }

    private void LaunchOne()
    {
        try { Persist(); new BrowserForm(_state, _store, _state.Settings.StartUrl).Show(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "設定錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task OpenManyAsync()
    {
        try
        {
            Persist();
            var limit = Math.Clamp(_state.Settings.BatchWindowLimit, 1, 6);
            var selected = _state.Settings.Slots.Where(x => x.Enabled)
                .OrderBy(x => x.Group).ThenBy(x => x.Number).Take(limit).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("目前沒有啟用的 Slot。請先在清單勾選要開啟的 Slot。", "批次開啟", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var slot in selected)
            {
                var url = string.IsNullOrWhiteSpace(slot.Url) ? _state.Settings.StartUrl : slot.Url;
                new BrowserForm(_state, _store, url).Show();
                await Task.Delay(180);
            }
            int ignored = _state.Settings.Slots.Count(x => x.Enabled) - selected.Count;
            if (ignored > 0) MessageBox.Show($"本次已開啟前 {limit} 個啟用 Slot；另有 {ignored} 個啟用 Slot 未開啟。", "批次開啟上限", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "批次開啟失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void AddSlot()
    {
        var current = _slotGrid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
        int group = current.Count == 0 ? 1 : current.Select(r => int.TryParse(Convert.ToString(r.Cells["Group"].Value), out int g) ? g : 1).Max();
        int next = current.Where(r => Convert.ToInt32(r.Cells["Group"].Value) == group).Select(r => Convert.ToInt32(r.Cells["Number"].Value)).DefaultIfEmpty(0).Max() + 1;
        _slotGrid.Rows.Add(false, group, next, _startUrl.Text.Trim(), false, 0);
    }

    private void RemoveSelectedSlot()
    {
        if (_slotGrid.CurrentRow is { IsNewRow: false } row) _slotGrid.Rows.Remove(row);
    }

    private static string NormalizeUrl(string value)
    {
        if (UrlHelper.TryNormalize(value, out var uri) && uri is not null) return uri.ToString();
        throw new InvalidOperationException($"網址格式不正確：{value}");
    }

    private static Button MakeButton(string text, bool primary) => new()
    {
        Text = text, AutoSize = true, Height = 34, FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Color.FromArgb(65, 110, 190) : Color.FromArgb(55, 59, 67),
        ForeColor = Color.White, Margin = new Padding(4)
    };
}
