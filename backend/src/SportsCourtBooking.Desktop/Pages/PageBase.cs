using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Desktop.Dialogs;

namespace SportsCourtBooking.Desktop.Pages;

/// <summary>Khung chung của mọi màn hình: tiêu đề, thanh công cụ, phần nội dung và xử lý lỗi.</summary>
public abstract class PageBase : UserControl
{
    private readonly FlowLayoutPanel _toolbar;
    private int _loadVersion;

    protected PageBase(AppServices app, IShell shell, string title, string subtitle)
    {
        App = app;
        Shell = shell;
        AutoScaleMode = AutoScaleMode.None;
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Font = Theme.Body;
        Padding = Ui.Pad(24, 18, 24, 18);

        Body = new Panel { Dock = DockStyle.Fill, Margin = Ui.Pad(0) };
        _toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Ui.Pad(0),
            Padding = Ui.Pad(0, 4, 0, 4)
        };
        var heading = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = Ui.Pad(0, 0, 0, 8)
        };
        heading.Controls.Add(Ui.Label(title, Theme.Title));
        heading.Controls.Add(Ui.Label(subtitle, Theme.Body, Theme.Muted));

        Controls.Add(Body);
        Controls.Add(_toolbar);
        Controls.Add(heading);
    }

    protected AppServices App { get; }

    protected IShell Shell { get; }

    protected Panel Body { get; }

    /// <summary>Hoàn tất khi lần tải gần nhất xong.</summary>
    public Task Loading { get; private set; } = Task.CompletedTask;

    public Task ReloadAsync() => Loading = GuardAsync(async () =>
    {
        var version = ++_loadVersion;
        // Chỉ hiển thị kết quả của lần tải mới nhất khi người dùng đổi bộ lọc liên tục.
        await LoadAsync(() => version == _loadVersion && !IsDisposed);
    });

    protected abstract Task LoadAsync(Func<bool> isCurrent);

    protected T Tool<T>(T control)
        where T : Control
    {
        _toolbar.Controls.Add(control);
        return control;
    }

    /// <summary>Đổi bộ lọc là tải lại ngay.</summary>
    protected ComboBox FilterCombo<T>(IEnumerable<Option<T>> options, T? selected = default, int width = 180)
    {
        var combo = Tool(Ui.Combo(options, selected, width));
        combo.SelectionChangeCommitted += async (_, _) => await ReloadAsync();
        return combo;
    }

    protected async Task GuardAsync(Func<Task> action)
    {
        UseWaitCursor = true;
        try
        {
            await action();
        }
        catch (AccountLockedException exception)
        {
            Shell.ForceSignOut(exception.Message);
        }
        catch (Exception exception)
        {
            if (!IsDisposed)
            {
                Shell.ShowMessage(ErrorText.For(exception), success: false);
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                UseWaitCursor = false;
            }
        }
    }

    /// <summary>Hỏi lại nếu cần, gọi nghiệp vụ, báo kết quả rồi tải lại màn hình.</summary>
    protected async Task ExecuteAsync(string? confirmation, Func<Task<(bool Success, string Message)>> action)
    {
        if (confirmation is not null && !Confirm(confirmation))
        {
            return;
        }

        await GuardAsync(async () =>
        {
            var (success, message) = await action();
            Shell.ShowMessage(message, success);
            await ReloadAsync();
        });
    }

    protected bool Confirm(string message)
        => MessageBox.Show(this, message, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    /// <summary>Mở hộp thoại; lưu thành công thì báo kết quả và tải lại màn hình.</summary>
    protected async Task ShowDialogAsync(FormDialog dialog)
    {
        using (dialog)
        {
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.Abort)
            {
                Shell.ForceSignOut(new AccountLockedException().Message);
                return;
            }

            if (result != DialogResult.OK)
            {
                return;
            }

            Shell.ShowMessage(dialog.ResultMessage, success: true);
        }

        await ReloadAsync();
    }

    /// <summary>Hộp thoại chỉ hỏi một đoạn nội dung (ví dụ lý do).</summary>
    protected Task PromptAsync(
        string title,
        string label,
        string submitText,
        Func<string, Task<(bool Success, string Message)>> submit,
        bool required = true,
        IReadOnlyList<string>? suggestions = null,
        string? note = null)
    {
        var dialog = new FormDialog(title, submitText, note);
        ComboBox? combo = suggestions is null
            ? null
            : dialog.AddCombo("Lý do có sẵn",
                suggestions.Select(text => new Option<string>(text, text)).Prepend(new Option<string>("", "— Tự nhập —")), "");
        var box = dialog.AddText(label, maxLength: 500, lines: 4);
        if (combo is not null)
        {
            combo.SelectionChangeCommitted += (_, _) => box.Text = Ui.Value<string>(combo);
        }

        dialog.OnSubmit(() => required && string.IsNullOrWhiteSpace(box.Text)
            ? Task.FromResult((false, $"Vui lòng nhập {label.ToLower(Display.Culture)}."))
            : submit(box.Text.Trim()));
        return ShowDialogAsync(dialog);
    }

    /// <summary>Đặt bảng vào khung trắng, có thể kèm tiêu đề nhỏ.</summary>
    protected static Panel GridCard(Control grid, string? title = null)
    {
        var card = Ui.Card(1);
        card.Dock = DockStyle.Fill;
        card.Controls.Add(grid);
        if (title is not null)
        {
            card.Controls.Add(SectionTitle(title));
        }

        return card;
    }

    protected static Label SectionTitle(string title) => new()
    {
        Text = title,
        Dock = DockStyle.Top,
        Height = Ui.S(42),
        Padding = Ui.Pad(12, 0, 12, 0),
        Font = Theme.Section,
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft,
        UseMnemonic = false
    };

    protected static Panel Gap(int height = 14) => new() { Dock = DockStyle.Top, Height = Ui.S(height) };

    /// <summary>Hai khung xếp trên dưới theo tỉ lệ, cách nhau một khoảng.</summary>
    protected static TableLayoutPanel Stack(Control top, Control bottom, float topPercent)
    {
        var stack = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, topPercent));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100 - topPercent));
        top.Margin = Ui.Pad(0, 0, 0, 14);
        bottom.Margin = Ui.Pad(0);
        stack.Controls.Add(top, 0, 0);
        stack.Controls.Add(bottom, 0, 1);
        return stack;
    }

    /// <summary>Hai khung xếp trái phải theo tỉ lệ, cách nhau một khoảng.</summary>
    protected static TableLayoutPanel Split(Control left, Control right, float leftPercent)
    {
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - leftPercent));
        left.Margin = Ui.Pad(0, 0, 14, 0);
        right.Margin = Ui.Pad(0);
        split.Controls.Add(left, 0, 0);
        split.Controls.Add(right, 1, 0);
        return split;
    }
}
