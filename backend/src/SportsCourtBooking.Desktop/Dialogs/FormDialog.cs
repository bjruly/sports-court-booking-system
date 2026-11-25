using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Dialogs;

/// <summary>
/// Hộp thoại nhập liệu dựng theo danh sách trường. Khi lưu không thành công, hộp thoại ở lại
/// và hiện thông báo ngay phía trên nút bấm để người dùng sửa.
/// </summary>
public sealed class FormDialog : Form
{
    private readonly TableLayoutPanel _fields;
    private readonly Label _error;
    private readonly Button _submit;
    private Func<Task<(bool Success, string Message)>>? _onSubmit;

    public FormDialog(string title, string submitText = "Lưu", string? note = null, int width = 540)
    {
        Text = title;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Surface;
        Font = Theme.Body;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = Ui.Pad(20),
            MinimumSize = new Size(Ui.S(width), 0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.S(width - 40)));

        if (note is not null)
        {
            var noteLabel = Ui.Label(note, Theme.Body, Theme.Muted);
            noteLabel.MaximumSize = new Size(Ui.S(width - 40), 0);
            noteLabel.Margin = Ui.Pad(0, 0, 0, 12);
            root.Controls.Add(noteLabel);
        }

        _fields = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Top,
            Margin = Ui.Pad(0)
        };
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.S(165)));
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(_fields);

        _error = Ui.Label(string.Empty, Theme.Body, Theme.Danger);
        _error.MaximumSize = new Size(Ui.S(width - 40), 0);
        _error.Margin = Ui.Pad(0, 8, 0, 4);
        _error.Visible = false;
        root.Controls.Add(_error);

        _submit = Ui.Button(submitText, async (_, _) => await SubmitAsync(), primary: true);
        var cancel = Ui.Button("Đóng", (_, _) => Close());
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Top,
            Margin = Ui.Pad(0, 10, 0, 0)
        };
        buttons.Controls.Add(_submit);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons);

        Controls.Add(root);
        AcceptButton = _submit;
        CancelButton = cancel;
    }

    /// <summary>Thông báo thành công do nghiệp vụ trả về, có sau khi ShowDialog trả về OK.</summary>
    public string ResultMessage { get; private set; } = string.Empty;

    public TextBox AddText(
        string label,
        string? value = null,
        int maxLength = 255,
        int lines = 1)
    {
        var box = new TextBox
        {
            Text = value ?? string.Empty,
            MaxLength = maxLength
        };
        if (lines > 1)
        {
            box.Multiline = true;
            box.AcceptsReturn = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.Height = Ui.S(22 * lines);
        }

        return AddField(label, box);
    }

    public ComboBox AddCombo<T>(string label, IEnumerable<Option<T>> options, T? selected = default)
        => AddField(label, Ui.Combo(options, selected));

    public DateTimePicker AddTime(string label, string value)
        => AddField(label, new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Value = DateTime.Today.Add(TimeSpan.TryParse(value, out var time) && time.Days == 0 ? time : TimeSpan.FromHours(6))
        });

    public static string TimeText(DateTimePicker picker)
        => picker.Value.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    public void OnSubmit(Func<Task<(bool Success, string Message)>> submit) => _onSubmit = submit;

    private TControl AddField<TControl>(string label, TControl control)
        where TControl : Control
    {
        var row = _fields.RowCount++;
        _fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var caption = Ui.Label(label, Theme.Body, Theme.Muted);
        caption.MaximumSize = new Size(Ui.S(155), 0);
        caption.Margin = Ui.Pad(0, 7, 8, 8);
        control.Dock = DockStyle.Top;
        control.Font = Theme.Body;
        control.Margin = Ui.Pad(0, 4, 0, 8);
        _fields.Controls.Add(caption, 0, row);
        _fields.Controls.Add(control, 1, row);
        return control;
    }

    private async Task SubmitAsync()
    {
        if (_onSubmit is null || !_submit.Enabled)
        {
            return;
        }

        _submit.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var (success, message) = await _onSubmit();
            if (success)
            {
                ResultMessage = message;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            ShowError(message);
        }
        catch (AccountLockedException)
        {
            DialogResult = DialogResult.Abort;
            Close();
        }
        catch (Exception exception)
        {
            ShowError(ErrorText.For(exception));
        }
        finally
        {
            UseWaitCursor = false;
            _submit.Enabled = true;
        }
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.Visible = true;
    }
}
