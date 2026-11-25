using System.Drawing.Drawing2D;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Controls;

public sealed record ChartPoint(string Label, decimal Value, string Tooltip);

/// <summary>Biểu đồ cột một chuỗi số liệu; rê chuột để xem giá trị từng cột.</summary>
public sealed class ColumnChart : Control
{
    private readonly ToolTip _toolTip = new();
    private IReadOnlyList<ChartPoint> _points = Array.Empty<ChartPoint>();
    private int _hover = -1;

    public ColumnChart()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Dock = DockStyle.Fill;
        BackColor = Theme.Surface;
    }

    public void SetPoints(IReadOnlyList<ChartPoint> points)
    {
        _points = points;
        _hover = -1;
        Invalidate();
    }

    private Rectangle Plot => new(Ui.S(64), Ui.S(22), Math.Max(1, Width - Ui.S(72)), Math.Max(1, Height - Ui.S(50)));

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Theme.Surface);
        if (_points.Count == 0)
        {
            return;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var plot = Plot;
        var max = NiceMax(_points.Max(point => point.Value));
        using (var gridPen = new Pen(Theme.Border))
        {
            for (var step = 0; step <= 4; step++)
            {
                var y = plot.Bottom - plot.Height * step / 4;
                graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                TextRenderer.DrawText(graphics, Compact(max * step / 4), Theme.Small,
                    new Rectangle(0, y - Ui.S(10), plot.Left - Ui.S(6), Ui.S(20)), Theme.Muted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
        }

        var band = (float)plot.Width / _points.Count;
        var barWidth = Math.Min(Ui.S(24), band * 0.6f);
        var peak = _points.Max(point => point.Value);
        for (var index = 0; index < _points.Count; index++)
        {
            var point = _points[index];
            var height = (float)(point.Value / max) * plot.Height;
            var x = plot.Left + band * index + (band - barWidth) / 2;
            if (height > 0)
            {
                using var brush = new SolidBrush(index == _hover ? ControlPaint.Dark(Theme.ChartGreen, 0.05f) : Theme.ChartGreen);
                using var path = RoundedTop(x, plot.Bottom - height, barWidth, height, Ui.S(4));
                graphics.FillPath(brush, path);
            }

            var bandArea = new Rectangle((int)(plot.Left + band * index), plot.Bottom + Ui.S(4), (int)band, Ui.S(20));
            TextRenderer.DrawText(graphics, point.Label, Theme.Small, bandArea, Theme.Muted,
                TextFormatFlags.HorizontalCenter);

            // Chỉ ghi trực tiếp giá trị cao nhất; các cột khác xem khi rê chuột.
            if (point.Value == peak && peak > 0)
            {
                TextRenderer.DrawText(graphics, Compact(point.Value), Theme.SmallBold,
                    new Rectangle(bandArea.X - Ui.S(10), (int)(plot.Bottom - height) - Ui.S(20), bandArea.Width + Ui.S(20), Ui.S(18)),
                    Theme.Text, TextFormatFlags.HorizontalCenter);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var plot = Plot;
        var index = _points.Count == 0 || e.X < plot.Left || e.X >= plot.Right
            ? -1
            : Math.Min(_points.Count - 1, (int)((e.X - plot.Left) / ((float)plot.Width / _points.Count)));
        if (index == _hover)
        {
            return;
        }

        _hover = index;
        _toolTip.SetToolTip(this, index < 0 ? string.Empty : _points[index].Tooltip);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = -1;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private static decimal NiceMax(decimal value)
    {
        if (value <= 0)
        {
            return 1_000_000m;
        }

        var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)value)));
        foreach (var factor in new[] { 1m, 2m, 4m, 5m, 8m, 10m })
        {
            if (value <= factor * magnitude)
            {
                return factor * magnitude;
            }
        }

        return 10m * magnitude;
    }

    private static string Compact(decimal value) => value switch
    {
        >= 1_000_000m => (value / 1_000_000m).ToString("0.##", Display.Culture) + " tr",
        >= 1_000m => (value / 1_000m).ToString("0.#", Display.Culture) + " N",
        _ => value.ToString("0", Display.Culture)
    };

    private static GraphicsPath RoundedTop(float x, float y, float width, float height, float radius)
    {
        radius = Math.Min(radius, Math.Min(width / 2, height));
        var path = new GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddLine(x + width, y + height, x, y + height);
        path.CloseFigure();
        return path;
    }
}

public sealed record Segment(string Label, int Value, Color Color);

/// <summary>Thanh tỉ lệ một dòng kèm chú giải có số lượng và phần trăm.</summary>
public sealed class SegmentBar : Control
{
    private IReadOnlyList<Segment> _segments = Array.Empty<Segment>();

    public SegmentBar()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Dock = DockStyle.Fill;
        BackColor = Theme.Surface;
    }

    public void SetSegments(params Segment[] segments)
    {
        _segments = segments;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Theme.Surface);
        var total = _segments.Sum(segment => segment.Value);
        var barHeight = Ui.S(16);
        using (var track = new SolidBrush(Theme.Border))
        {
            if (total == 0)
            {
                graphics.FillRectangle(track, 0, 0, Width, barHeight);
            }
        }

        float x = 0;
        var gap = Ui.S(2);
        var visible = _segments.Where(segment => segment.Value > 0).ToList();
        var usable = Width - gap * Math.Max(0, visible.Count - 1);
        foreach (var segment in visible)
        {
            var width = (float)segment.Value / total * usable;
            using var brush = new SolidBrush(segment.Color);
            graphics.FillRectangle(brush, x, 0, width, barHeight);
            x += width + gap;
        }

        var y = barHeight + Ui.S(12);
        foreach (var segment in _segments)
        {
            using var brush = new SolidBrush(segment.Color);
            graphics.FillRectangle(brush, 0, y + Ui.S(5), Ui.S(10), Ui.S(10));
            var percent = total == 0 ? 0 : (double)segment.Value / total * 100;
            var row = new Rectangle(Ui.S(18), y, Math.Max(1, Width - Ui.S(18)), Ui.S(20));
            TextRenderer.DrawText(graphics, segment.Label, Theme.Body, row, Theme.Text, TextFormatFlags.Left);
            TextRenderer.DrawText(graphics, $"{segment.Value} · {Display.Percent(percent)}", Theme.Body, row,
                Theme.Muted, TextFormatFlags.Right);
            y += Ui.S(26);
        }
    }
}
