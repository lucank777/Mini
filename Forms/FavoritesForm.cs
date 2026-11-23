using MiniGameBrowser.Core;
using MiniGameBrowser.Models;

namespace MiniGameBrowser.Forms;

public sealed class FavoritesForm : Form
{
    private readonly AppState _state;
    private readonly JsonStore _store;
    private readonly Action<string> _open;
    private readonly ListView _list = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = true, HideSelection = false };

    public FavoritesForm(AppState state, JsonStore store, Action<string> open)
    {
        _state = state; _store = store; _open = open;
        Text = "收藏管理"; Width = 720; Height = 480; StartPosition = FormStartPosition.CenterParent;
        _list.Columns.Add("名稱", 220); _list.Columns.Add("網址", 440);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        var openButton = Button("開啟選取"); openButton.Click += (_, _) => OpenSelected();
        var remove = Button("刪除選取"); remove.Click += (_, _) => DeleteSelected();
        var reload = Button("重新載入"); reload.Click += (_, _) => ReloadList();
        bar.Controls.AddRange([openButton, remove, reload]);
        Controls.Add(_list); Controls.Add(bar);
        _list.DoubleClick += (_, _) => OpenSelected();
        ReloadList();
    }

    private static Button Button(string text) => new() { Text = text, AutoSize = true, Height = 30 };
    private void ReloadList()
    {
        _list.BeginUpdate(); _list.Items.Clear();
        foreach (var f in _state.Favorites)
        {
            var item = new ListViewItem(f.Title); item.SubItems.Add(f.Url); item.Tag = f; _list.Items.Add(item);
        }
        _list.EndUpdate();
    }
    private void OpenSelected()
    {
        foreach (ListViewItem item in _list.SelectedItems)
            if (item.Tag is FavoriteItem favorite) _open(favorite.Url);
    }
    private void DeleteSelected()
    {
        var selected = _list.SelectedItems.Cast<ListViewItem>().Select(x => x.Tag).OfType<FavoriteItem>().ToHashSet();
        _state.Favorites.RemoveAll(x => selected.Contains(x)); _store.Save(_state); ReloadList();
    }
}
