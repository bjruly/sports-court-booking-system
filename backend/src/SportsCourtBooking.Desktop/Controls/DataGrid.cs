using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Controls;

public sealed class GridColumn<T>
{
    public required string Header { get; init; }

    public required Func<T, string> Text { get; init; }

    /// <summary>Khóa sắp xếp; bỏ trống thì sắp xếp theo chữ hiển thị.</summary>
    public Func<T, IComparable?>? SortKey { get; init; }

    public Func<T, Color>? Color { get; init; }

    public int Width { get; init; } = 100;

    public bool AlignRight { get; init; }

    public bool Bold { get; init; }
}

/// <summary>Bảng chỉ đọc, chọn cả dòng, bấm tiêu đề cột để sắp xếp.</summary>
public sealed class DataGrid<T> : DataGridView, IPrintableGrid
    where T : class
{
    private readonly List<GridColumn<T>> _columns = new();
    private readonly Func<T, object>? _key;
    private List<T> _items = new();
    private int _sortIndex = -1;
    private bool _sortDescending;

    public DataGrid(Func<T, object>? key = null)
    {
        _key = key;
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        ReadOnly = true;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        AllowUserToOrderColumns = false;
        RowHeadersVisible = false;
        MultiSelect = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        BackgroundColor = Theme.Surface;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        GridColor = Theme.Border;
        EnableHeadersVisualStyles = false;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Background,
            ForeColor = Theme.Muted,
            SelectionBackColor = Theme.Background,
            SelectionForeColor = Theme.Muted,
            Font = Theme.SmallBold,
            Padding = Ui.Pad(6, 8, 6, 8),
            WrapMode = DataGridViewTriState.True
        };
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            SelectionBackColor = Theme.Selection,
            SelectionForeColor = Theme.Text,
            Font = Theme.Body,
            Padding = Ui.Pad(6, 7, 6, 7),
            WrapMode = DataGridViewTriState.True,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        ColumnHeaderMouseClick += (_, e) => SortBy(e.ColumnIndex);
    }

    public string EmptyText { get; set; } = "Chưa có dữ liệu.";

    public IReadOnlyList<T> Items => _items;

    public T? Selected => SelectedRows.Count > 0 ? SelectedRows[0].Tag as T : null;

    public DataGrid<T> Add(
        string header,
        Func<T, string> text,
        int width = 100,
        bool right = false,
        bool bold = false,
        Func<T, Color>? color = null,
        Func<T, IComparable?>? sort = null)
    {
        _columns.Add(new GridColumn<T>
        {
            Header = header, Text = text, Width = width, AlignRight = right, Bold = bold, Color = color, SortKey = sort
        });
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            FillWeight = width,
            MinimumWidth = Ui.S(Math.Min(width, 60)),
            SortMode = DataGridViewColumnSortMode.Programmatic
        };
        if (right)
        {
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        if (bold)
        {
            column.DefaultCellStyle.Font = Theme.BodyBold;
        }

        Columns.Add(column);
        return this;
    }

    public void SetItems(IEnumerable<T> items)
    {
        _items = items.ToList();
        Fill();
    }

    public IReadOnlyList<(string Header, int Width, bool Right)> PrintColumns
        => _columns.Select(column => (column.Header, column.Width, column.AlignRight)).ToList();

    public IEnumerable<string[]> PrintRows
        => SortedItems().Select(item => _columns.Select(column => column.Text(item)).ToArray());

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Rows.Count == 0)
        {
            var area = new Rectangle(0, ColumnHeadersHeight, Width, Math.Max(Ui.S(60), Height - ColumnHeadersHeight));
            TextRenderer.DrawText(e.Graphics, EmptyText, Theme.Body, area, Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }
    }

    private void SortBy(int columnIndex)
    {
        _sortDescending = _sortIndex == columnIndex && !_sortDescending;
        _sortIndex = columnIndex;
        Fill();
    }

    private IEnumerable<T> SortedItems()
    {
        if (_sortIndex < 0)
        {
            return _items;
        }

        var column = _columns[_sortIndex];
        if (column.SortKey is not null)
        {
            return _sortDescending ? _items.OrderByDescending(column.SortKey) : _items.OrderBy(column.SortKey);
        }

        var comparer = StringComparer.Create(Display.Culture, ignoreCase: true);
        return _sortDescending
            ? _items.OrderByDescending(column.Text, comparer)
            : _items.OrderBy(column.Text, comparer);
    }

    private void Fill()
    {
        var selectedKey = Selected is { } selected && _key is not null ? _key(selected) : null;
        SuspendLayout();
        Rows.Clear();
        foreach (var item in SortedItems())
        {
            var row = new DataGridViewRow { Tag = item };
            row.CreateCells(this, _columns.Select(column => (object)column.Text(item)).ToArray());
            for (var index = 0; index < _columns.Count; index++)
            {
                if (_columns[index].Color is { } color)
                {
                    var cellColor = color(item);
                    row.Cells[index].Style.ForeColor = cellColor;
                    row.Cells[index].Style.SelectionForeColor = cellColor;
                }
            }

            Rows.Add(row);
        }

        for (var index = 0; index < Columns.Count; index++)
        {
            Columns[index].HeaderCell.SortGlyphDirection = index != _sortIndex
                ? SortOrder.None
                : _sortDescending ? SortOrder.Descending : SortOrder.Ascending;
        }

        ClearSelection();
        CurrentCell = null;
        if (selectedKey is not null)
        {
            foreach (DataGridViewRow row in Rows)
            {
                if (_key!((T)row.Tag!).Equals(selectedKey))
                {
                    CurrentCell = row.Cells[0];
                    row.Selected = true;
                    break;
                }
            }
        }

        ResumeLayout();
        Invalidate();
        OnSelectionChanged(EventArgs.Empty);
    }
}
