using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Controls;

/// <summary>Ô số liệu: nhãn, con số lớn và một dòng chú thích.</summary>
public sealed class StatCard : Control
{
    private string _value = "–";
    private string _caption = string.Empty;

    public StatCard(string label)
    {
        Text = label;
        DoubleBuffered = true;
        ResizeRedraw = true;
        Dock = DockStyle.Fill;
        Margin = Ui.Pad(0, 0, 12, 0);
    }

    public void Set(string value, string caption = "")
    {
        _value = value;
        _caption = caption;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Theme.Surface);
        using (var pen = new Pen(Theme.Border))
        {
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        var left = Ui.S(14);
        var width = Width - left * 2;
        const TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(e.Graphics, Text, Theme.Small,
            new Rectangle(left, Ui.S(12), width, Ui.S(20)), Theme.Muted, flags);
        TextRenderer.DrawText(e.Graphics, _value, Theme.Stat,
            new Rectangle(left, Ui.S(32), width, Ui.S(38)), Theme.Text, flags);
        TextRenderer.DrawText(e.Graphics, _caption, Theme.Small,
            new Rectangle(left, Ui.S(72), width, Ui.S(20)), Theme.Muted, flags);
    }

    /// <summary>Một hàng các ô số liệu chia đều chiều ngang.</summary>
    public static TableLayoutPanel Row(params StatCard[] cards)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Ui.S(100),
            ColumnCount = cards.Length,
            RowCount = 1,
            Margin = Ui.Pad(0),
            Padding = Ui.Pad(0)
        };
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (var index = 0; index < cards.Length; index++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cards.Length));
            if (index == cards.Length - 1)
            {
                cards[index].Margin = Ui.Pad(0);
            }

            row.Controls.Add(cards[index], index, 0);
        }

        return row;
    }
}
