using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages;

/// <summary>Màn hình danh sách: một bảng và các nút thao tác theo dòng đang chọn.</summary>
public abstract class ListPage<T> : PageBase
    where T : class
{
    private readonly List<(Button Button, Func<T, bool> CanRun)> _actions = new();
    private readonly ContextMenuStrip _menu = new();

    protected ListPage(AppServices app, IShell shell, string title, string subtitle, Func<T, object> key)
        : base(app, shell, title, subtitle)
    {
        Grid = new DataGrid<T>(key);
        Grid.SelectionChanged += (_, _) => UpdateActions();
        Grid.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                Grid.CurrentCell = Grid.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
            }
        };
        _menu.Opening += (_, e) =>
        {
            _menu.Items.Clear();
            foreach (var (button, _) in _actions.Where(action => action.Button.Enabled))
            {
                _menu.Items.Add(button.Text, null, (_, _) => button.PerformClick());
            }

            e.Cancel = _menu.Items.Count == 0;
        };
        Grid.ContextMenuStrip = _menu;
    }

    protected DataGrid<T> Grid { get; }

    protected void AddAction(
        string text, Func<T, Task> run, Func<T, bool>? canRun = null, bool primary = false, bool danger = false)
    {
        var button = Tool(Ui.Button(text, async (_, _) =>
        {
            if (Grid.Selected is { } item)
            {
                await run(item);
            }
        }, primary, danger));
        button.Enabled = false;
        _actions.Add((button, canRun ?? (_ => true)));
    }

    protected void AddCommand(string text, Func<Task> run, bool primary = false)
        => Tool(Ui.Button(text, async (_, _) => await run(), primary));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateActions()
    {
        var selected = Grid.Selected;
        foreach (var (button, canRun) in _actions)
        {
            button.Enabled = selected is not null && canRun(selected);
        }
    }
}
