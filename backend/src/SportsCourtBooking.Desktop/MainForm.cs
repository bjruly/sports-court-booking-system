using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Desktop.Pages;

namespace SportsCourtBooking.Desktop;

/// <summary>Cửa sổ chính: menu trái, thanh trên cùng và vùng nội dung.</summary>
public sealed class MainForm : Form, IShell
{
    private const int NotificationIntervalMs = 30_000;

    private readonly AppServices _app;
    private readonly IReadOnlyList<PageEntry> _pages = PageCatalog.All;
    private readonly Dictionary<string, Button> _menu = new();
    private readonly ReadNotifications _read;
    private readonly ContextMenuStrip _notificationMenu = new() { ShowImageMargin = false };
    private readonly System.Windows.Forms.Timer _notificationTimer = new() { Interval = NotificationIntervalMs };
    private readonly System.Windows.Forms.Timer _messageTimer = new();
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Theme.Background };
    private readonly Label _message;
    private readonly Button _notifications;
    private List<NotificationItemDto> _notificationItems = new();

    public MainForm(AppServices app)
    {
        _app = app;
        var user = app.CurrentUser ?? throw new InvalidOperationException("Chưa đăng nhập.");
        _read = new ReadNotifications(user.Id);

        Text = "Sân Nhà – Quản trị viên";
        AutoScaleMode = AutoScaleMode.None;
        Font = Theme.Body;
        BackColor = Theme.Background;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(Ui.S(1080), Ui.S(680));
        Size = new Size(Ui.S(1360), Ui.S(820));
        KeyPreview = true;

        _notifications = Ui.Button("Thông báo", (_, _) => ShowNotifications());
        _message = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 0,
            Visible = false,
            Font = Theme.BodyBold,
            ForeColor = Color.White,
            Padding = Ui.Pad(24, 8, 24, 8),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
            Cursor = Cursors.Hand
        };
        _message.Click += (_, _) => HideMessage();

        Controls.Add(_content);
        Controls.Add(_message);
        Controls.Add(BuildHeader(user));
        Controls.Add(BuildSidebar());

        _messageTimer.Tick += (_, _) => HideMessage();
        _notificationTimer.Tick += async (_, _) => await LoadNotificationsAsync();
        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.F5 && CurrentPage is { } page)
            {
                e.Handled = true;
                await page.ReloadAsync();
            }
        };
        Shown += async (_, _) =>
        {
            Navigate(_pages[0].Key);
            _notificationTimer.Start();
            await LoadNotificationsAsync();
        };
    }

    /// <summary>Đóng để quay về màn hình đăng nhập thay vì thoát chương trình.</summary>
    public bool SignedOut { get; private set; }

    public string? SignOutReason { get; private set; }

    public PageBase? CurrentPage { get; private set; }

    public void Navigate(string pageKey)
    {
        var entry = _pages.FirstOrDefault(page => page.Key == pageKey);
        if (entry is null)
        {
            return;
        }

        foreach (var (key, button) in _menu)
        {
            var active = key == pageKey;
            button.BackColor = active ? Theme.SidebarActive : Theme.Sidebar;
            button.ForeColor = active ? Color.White : Theme.SidebarText;
            button.Font = active ? Theme.BodyBold : Theme.Body;
        }

        var previous = CurrentPage;
        var page = entry.Create(_app, this);
        CurrentPage = page;
        _content.Controls.Add(page);
        page.BringToFront();
        if (previous is not null)
        {
            _content.Controls.Remove(previous);
            // Hủy sau khi sự kiện đang chạy trên màn hình cũ kết thúc.
            BeginInvoke(previous.Dispose);
        }

        _ = page.ReloadAsync();
    }

    public void ShowMessage(string message, bool success)
    {
        _message.Text = message;
        _message.BackColor = success ? Theme.Success : Theme.Danger;
        var textSize = TextRenderer.MeasureText(message, _message.Font,
            new Size(Math.Max(1, _content.Width - _message.Padding.Horizontal), 0), TextFormatFlags.WordBreak);
        _message.Height = textSize.Height + _message.Padding.Vertical;
        _message.Visible = true;
        _messageTimer.Stop();
        _messageTimer.Interval = success ? 4_000 : 10_000;
        _messageTimer.Start();
    }

    public void ForceSignOut(string reason)
    {
        if (SignedOut || IsDisposed)
        {
            return;
        }

        SignOutReason = reason;
        SignOut();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _notificationTimer.Stop();
        _messageTimer.Stop();
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notificationTimer.Dispose();
            _messageTimer.Dispose();
            _notificationMenu.Dispose();
        }

        base.Dispose(disposing);
    }

    private void SignOut()
    {
        SignedOut = true;
        _app.SignOut();
        Close();
    }

    private void HideMessage()
    {
        _messageTimer.Stop();
        _message.Visible = false;
    }

    private Control BuildHeader(UserDto user)
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Ui.S(58),
            BackColor = Theme.Surface,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Ui.Pad(24, 11, 16, 0)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var identity = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Ui.Pad(0) };
        var name = Ui.Label(user.FullName, Theme.BodyBold);
        name.Margin = Ui.Pad(0, 8, 8, 0);
        var role = Ui.Label("Quản trị viên", Theme.Body, Theme.Muted);
        role.Margin = Ui.Pad(0, 8, 0, 0);
        identity.Controls.Add(name);
        identity.Controls.Add(role);

        var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Ui.Pad(0) };
        actions.Controls.Add(Ui.Button("Làm mới (F5)", async (_, _) =>
        {
            if (CurrentPage is { } page)
            {
                await page.ReloadAsync();
            }
        }));
        actions.Controls.Add(_notifications);
        actions.Controls.Add(Ui.Button("Đăng xuất", (_, _) => SignOut()));

        header.Controls.Add(identity, 0, 0);
        header.Controls.Add(actions, 1, 0);
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        return header;
    }

    private Control BuildSidebar()
    {
        var sidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = Ui.S(236),
            BackColor = Theme.Sidebar,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = Ui.Pad(0, 14, 0, 14)
        };
        var brand = Ui.Label("SÂN NHÀ.", new Font("Segoe UI", 16f, FontStyle.Bold), Color.White);
        brand.Margin = Ui.Pad(20, 0, 0, 12);
        sidebar.Controls.Add(brand);

        string? group = null;
        foreach (var page in _pages)
        {
            if (page.Group != group)
            {
                group = page.Group;
                var caption = Ui.Label(group, Theme.SmallBold, Theme.SidebarMuted);
                caption.Margin = Ui.Pad(20, 14, 0, 4);
                sidebar.Controls.Add(caption);
            }

            var button = new Button
            {
                Text = page.Title,
                Width = Ui.S(220),
                Height = Ui.S(36),
                Margin = Ui.Pad(8, 0, 8, 2),
                Padding = Ui.Pad(10, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Sidebar,
                ForeColor = Theme.SidebarText,
                Font = Theme.Body,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Theme.SidebarActive;
            button.Click += (_, _) => Navigate(page.Key);
            _menu[page.Key] = button;
            sidebar.Controls.Add(button);
        }

        return sidebar;
    }

    private async Task LoadNotificationsAsync()
    {
        if (_app.CurrentUser is not { } user)
        {
            return;
        }

        try
        {
            _notificationItems = await _app.CallAsync((INotificationService service) =>
                service.GetNotificationsAsync(user.Id, user.Role));
            if (!IsDisposed)
            {
                UpdateNotificationBadge();
            }
        }
        catch (AccountLockedException exception)
        {
            ForceSignOut(exception.Message);
        }
        catch (Exception)
        {
            // Chạy ngầm định kỳ: mất kết nối tạm thời thì giữ danh sách cũ, lần sau thử lại.
        }
    }

    private void UpdateNotificationBadge()
    {
        var unread = _notificationItems.Count(item => !_read.IsRead(item.Id));
        _notifications.Text = unread == 0 ? "Thông báo" : $"Thông báo ({unread})";
    }

    private void ShowNotifications()
    {
        _notificationMenu.Items.Clear();
        if (_notificationItems.Count == 0)
        {
            _notificationMenu.Items.Add(new ToolStripMenuItem("Chưa có thông báo mới") { Enabled = false });
        }

        foreach (var item in _notificationItems)
        {
            var unread = !_read.IsRead(item.Id);
            var entry = new ToolStripMenuItem($"{item.Title} · {item.TimeAgo}{Environment.NewLine}{item.Message}".Replace("&", "&&"))
            {
                Font = unread ? Theme.BodyBold : Theme.Body,
                Padding = Ui.Pad(4, 6, 4, 6)
            };
            entry.Click += (_, _) => Navigate(item.Url);
            _notificationMenu.Items.Add(entry);
        }

        _read.MarkRead(_notificationItems.Select(item => item.Id));
        UpdateNotificationBadge();
        _notificationMenu.Show(_notifications, new Point(_notifications.Width, _notifications.Height),
            ToolStripDropDownDirection.BelowLeft);
    }
}
