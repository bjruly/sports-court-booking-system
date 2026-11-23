namespace SportsCourtBooking.Desktop.Core;

/// <summary>Hàm tạo control theo giao diện chung. Kích thước viết ở 96 DPI rồi nhân tỉ lệ màn hình.</summary>
public static class Ui
{
    private static readonly float ScaleFactor = GetScale();

    public static int S(int pixels) => (int)Math.Round(pixels * ScaleFactor);

    public static Padding Pad(int all) => new(S(all));

    public static Padding Pad(int left, int top, int right, int bottom) => new(S(left), S(top), S(right), S(bottom));

    public static Label Label(string text, Font? font = null, Color? color = null) => new()
    {
        Text = text,
        AutoSize = true,
        Font = font ?? Theme.Body,
        ForeColor = color ?? Theme.Text,
        Margin = Pad(0, 0, 0, 2),
        UseMnemonic = false
    };

    public static Button Button(string text, EventHandler onClick, bool primary = false, bool danger = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, S(34)),
            Padding = Pad(10, 0, 10, 0),
            Margin = Pad(0, 0, 8, 8),
            FlatStyle = FlatStyle.Flat,
            Font = Theme.Body,
            Cursor = Cursors.Hand,
            UseMnemonic = false
        };
        button.Click += onClick;

        void ApplyColors()
        {
            var active = button.Enabled;
            button.BackColor = !active ? Theme.Background : primary ? Theme.Primary : Theme.Surface;
            button.ForeColor = !active ? Theme.Muted : primary ? Color.White : danger ? Theme.Danger : Theme.Text;
            button.FlatAppearance.BorderColor =
                !active ? Theme.Border : primary ? Theme.Primary : danger ? Theme.Danger : Theme.Border;
        }

        button.EnabledChanged += (_, _) => ApplyColors();
        ApplyColors();
        return button;
    }

    public static ComboBox Combo<T>(IEnumerable<Option<T>> options, T? selected = default, int width = 180)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = S(width),
            Font = Theme.Body,
            Margin = Pad(0, 4, 10, 8)
        };
        combo.Items.AddRange(options.Cast<object>().ToArray());
        Select(combo, selected);
        return combo;
    }

    /// <summary>Thay danh sách lựa chọn nhưng giữ lựa chọn hiện tại nếu còn.</summary>
    public static void SetOptions<T>(ComboBox combo, IEnumerable<Option<T>> options)
    {
        var current = Value<T>(combo);
        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.AddRange(options.Cast<object>().ToArray());
        Select(combo, current);
        combo.EndUpdate();
    }

    public static void Select<T>(ComboBox combo, T? value)
    {
        for (var index = 0; index < combo.Items.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(((Option<T>)combo.Items[index]!).Value, value!))
            {
                combo.SelectedIndex = index;
                return;
            }
        }

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    public static T Value<T>(ComboBox combo) => combo.SelectedItem is Option<T> option ? option.Value : default!;

    public static TextBox SearchBox(string placeholder, Func<Task> onSearch, int width = 260)
    {
        var box = new TextBox
        {
            PlaceholderText = placeholder,
            Width = S(width),
            Font = Theme.Body,
            Margin = Pad(0, 4, 10, 8)
        };
        box.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await onSearch();
            }
        };
        return box;
    }

    public static DateTimePicker DatePicker(DateOnly date) => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Value = date.ToDateTime(TimeOnly.MinValue),
        Width = S(150),
        Font = Theme.Body,
        Margin = Pad(0, 4, 10, 8)
    };

    public static DateOnly DateOf(DateTimePicker picker) => DateOnly.FromDateTime(picker.Value);

    /// <summary>Khung trắng có viền mảnh, dùng để gom một nhóm nội dung.</summary>
    public static Panel Card(int padding = 16)
    {
        var panel = new Panel { BackColor = Theme.Surface, Padding = Pad(padding), Margin = Pad(0) };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
        panel.Resize += (_, _) => panel.Invalidate();
        return panel;
    }

    private static float GetScale()
    {
        using var graphics = Graphics.FromHwnd(IntPtr.Zero);
        return graphics.DpiX / 96f;
    }
}

public sealed record Option<T>(T Value, string Text)
{
    public override string ToString() => Text;
}
