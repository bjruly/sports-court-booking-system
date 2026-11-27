using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class ActivityHistoryPage : ListPage<ActivityHistoryItem>
{
    private readonly ComboBox _role;
    private readonly DateTimePicker _date;
    private readonly Button _previous;
    private readonly Button _next;
    private readonly Label _pageLabel;
    private UserRole? _selectedRole;
    private DateOnly? _selectedDate;
    private int _page = 1;

    public ActivityHistoryPage(AppServices app, IShell shell)
        : base(app, shell, "Lịch sử thao tác", "Thao tác của quản trị viên và chủ sân.", item => item.Id)
    {
        Grid.EmptyText = "Không có thao tác nào phù hợp.";
        Grid.Add("Thời gian (giờ VN)", item => VietnamClock.ToVietnamTime(item.OccurredAtUtc)
                .ToString("dd/MM/yyyy HH:mm:ss", Display.Culture), 148, sort: item => item.OccurredAtUtc)
            .Add("Người thực hiện", item => string.IsNullOrWhiteSpace(item.ActorName)
                ? $"Tài khoản #{item.ActorId}" : $"{item.ActorName} (#{item.ActorId})", 165)
            .Add("Vai trò", item => item.ActorRole == UserRole.Admin ? "Quản trị viên" : "Chủ sân", 105)
            .Add("Thao tác", item => item.Action, 195, bold: true)
            .Add("Đối tượng", item => item.Target, 190);

        _role = Tool(Ui.Combo(new[]
        {
            new Option<UserRole?>(null, "Tất cả vai trò"),
            new Option<UserRole?>(UserRole.Admin, "Quản trị viên"),
            new Option<UserRole?>(UserRole.CourtOwner, "Chủ sân")
        }));
        Tool(Ui.Label("Lọc theo ngày", Theme.Body));
        _date = Tool(Ui.DatePicker(VietnamClock.Today));
        _date.ShowCheckBox = true;
        _date.Checked = false;
        _date.AccessibleName = "Ngày thao tác";
        AddCommand("Lọc", ApplyFiltersAsync, primary: true);

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = Ui.Pad(0, 8, 0, 0)
        };
        _previous = Ui.Button("Trước", async (_, _) => await MovePageAsync(-1));
        _next = Ui.Button("Sau", async (_, _) => await MovePageAsync(1));
        _previous.Enabled = _next.Enabled = false;
        _pageLabel = Ui.Label("Trang 1 / 1", Theme.Body);
        navigation.Controls.AddRange(new Control[] { _previous, _pageLabel, _next });

        Body.Controls.Add(GridCard(Grid));
        Body.Controls.Add(navigation);
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var adminId = App.UserId;
        var role = _selectedRole;
        var date = _selectedDate;
        var page = _page;
        var result = await App.CallAsync((IActivityHistoryService service) =>
            service.GetPageAsync(adminId, role, date, page));
        if (!isCurrent())
        {
            return;
        }

        _page = result.Page;
        Grid.SetItems(result.Items);
        _pageLabel.Text = $"Trang {result.Page} / {result.PageCount}";
        _previous.Enabled = result.Page > 1;
        _next.Enabled = result.Page < result.PageCount;
    }

    private Task ApplyFiltersAsync()
    {
        _selectedRole = Ui.Value<UserRole?>(_role);
        _selectedDate = _date.Checked ? Ui.DateOf(_date) : null;
        _page = 1;
        return ReloadAsync();
    }

    private Task MovePageAsync(int offset)
    {
        _page += offset;
        return ReloadAsync();
    }
}
