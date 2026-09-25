namespace SrvDesk;

/// <summary>
/// ListView 工具栏操作的选中兜底：按钮抢焦点时 SelectedItems 可能暂时为空。
/// 仅在确有选中时更新缓存，空选中不覆盖。
/// </summary>
internal sealed class ListViewStickySelection<T> where T : class
{
    private readonly ListView _list;
    private readonly Func<object?, T?> _asT;
    private List<T> _sticky = [];

    public ListViewStickySelection(ListView list, Func<object?, T?>? asT = null)
    {
        _list = list;
        _asT = asT ?? (tag => tag as T);
        list.ItemSelectionChanged += (_, e) =>
        {
            if (e.IsSelected)
                Capture();
        };
        list.SelectedIndexChanged += (_, _) =>
        {
            if (list.SelectedItems.Count > 0)
                Capture();
        };
        list.MouseUp += (_, _) => Capture();
        list.KeyUp += (_, _) => Capture();
    }

    public void Capture()
    {
        var current = ReadSelected();
        if (current.Count > 0)
            _sticky = current;
    }

    public IReadOnlyList<T> Get()
    {
        var current = ReadSelected();
        return current.Count > 0 ? current : _sticky;
    }

    public void BindToolbarButton(Button button, Action click)
    {
        ThemedSettingsChrome.PreventFocusSteal(button);
        button.MouseDown += (_, _) => Capture();
        button.Click += (_, _) =>
        {
            Capture();
            click();
        };
    }

    private List<T> ReadSelected()
    {
        var list = new List<T>();
        foreach (ListViewItem row in _list.SelectedItems)
        {
            var item = _asT(row.Tag);
            if (item is not null)
                list.Add(item);
        }
        return list;
    }
}
