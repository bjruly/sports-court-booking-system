using System.Drawing.Printing;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Controls;

public interface IPrintableGrid
{
    IReadOnlyList<(string Header, int Width, bool Right)> PrintColumns { get; }

    IEnumerable<string[]> PrintRows { get; }
}

/// <summary>In báo cáo: tiêu đề, các dòng tóm tắt rồi lần lượt từng bảng, tự sang trang.</summary>
public sealed class GridPrinter
{
    private readonly string _title;
    private readonly IReadOnlyList<string> _summary;
    private readonly IReadOnlyList<(string Title, IPrintableGrid Grid)> _sections;
    private List<Line> _lines = new();
    private int _position;

    public GridPrinter(
        string title,
        IReadOnlyList<string> summary,
        params (string Title, IPrintableGrid Grid)[] sections)
    {
        _title = title;
        _summary = summary;
        _sections = sections;
    }

    public void Preview(IWin32Window owner)
    {
        using var document = CreateDocument();
        using var preview = new PrintPreviewDialog
        {
            Document = document,
            Width = Ui.S(1000),
            Height = Ui.S(760),
            StartPosition = FormStartPosition.CenterParent,
            ShowIcon = false,
            Text = _title
        };
        try
        {
            preview.ShowDialog(owner);
        }
        catch (InvalidPrinterException)
        {
            MessageBox.Show(owner, "Máy chưa cài máy in nào nên không xem trước bản in được.", "In báo cáo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public PrintDocument CreateDocument()
    {
        var document = new PrintDocument { DocumentName = _title };
        document.DefaultPageSettings.Landscape = true;
        document.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
        document.BeginPrint += (_, _) =>
        {
            _lines = BuildLines();
            _position = 0;
        };
        document.PrintPage += PrintPage;
        return document;
    }

    private List<Line> BuildLines()
    {
        var lines = new List<Line> { new(_title, null, null, Theme.Title) };
        lines.AddRange(_summary.Select(text => new Line(text, null, null, Theme.Body)));
        foreach (var (title, grid) in _sections)
        {
            lines.Add(new Line(title, null, null, Theme.Section));
            lines.Add(new Line(null, grid.PrintColumns.Select(column => column.Header).ToArray(), grid, Theme.SmallBold));
            lines.AddRange(grid.PrintRows.Select(cells => new Line(null, cells, grid, Theme.Small)));
        }

        return lines;
    }

    private void PrintPage(object sender, PrintPageEventArgs e)
    {
        var graphics = e.Graphics!;
        float y = e.MarginBounds.Top;
        while (_position < _lines.Count)
        {
            var line = _lines[_position];
            var height = Measure(graphics, line, e.MarginBounds.Width);
            // Luôn in ít nhất một dòng mỗi trang để không lặp vô hạn khi một dòng cao hơn trang.
            if (y + height > e.MarginBounds.Bottom && y > e.MarginBounds.Top)
            {
                e.HasMorePages = true;
                return;
            }

            Draw(graphics, line, e.MarginBounds.Left, y, e.MarginBounds.Width, height);
            y += height;
            _position++;
        }

        e.HasMorePages = false;
    }

    private static float Measure(Graphics graphics, Line line, float width)
    {
        if (line.Cells is null)
        {
            return graphics.MeasureString(line.Text, line.Font, (int)width).Height + 8;
        }

        var widths = Widths(line.Grid!, width);
        return line.Cells
            .Select((text, index) => graphics.MeasureString(text, line.Font, (int)widths[index]).Height)
            .Max() + 8;
    }

    private static void Draw(Graphics graphics, Line line, float left, float y, float width, float height)
    {
        if (line.Cells is null)
        {
            graphics.DrawString(line.Text, line.Font, Brushes.Black, new RectangleF(left, y, width, height));
            return;
        }

        var widths = Widths(line.Grid!, width);
        var columns = line.Grid!.PrintColumns;
        for (var index = 0; index < line.Cells.Length; index++)
        {
            using var format = new StringFormat
            {
                Alignment = columns[index].Right ? StringAlignment.Far : StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString(line.Cells[index], line.Font, Brushes.Black,
                new RectangleF(left, y, widths[index], height), format);
            left += widths[index];
        }

        graphics.DrawLine(Pens.LightGray, left - width, y + height, left, y + height);
    }

    private static float[] Widths(IPrintableGrid grid, float total)
    {
        float sum = grid.PrintColumns.Sum(column => column.Width);
        return grid.PrintColumns.Select(column => column.Width / sum * total).ToArray();
    }

    private sealed record Line(string? Text, string[]? Cells, IPrintableGrid? Grid, Font Font);
}
