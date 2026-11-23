using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class OwnersPage : ListPage<CourtOwnerItemDto>
{
    private readonly StatCard _total = new("Tổng chủ sân");
    private readonly StatCard _active = new("Đang hoạt động");
    private readonly StatCard _locked = new("Đang bị khóa");
    private readonly StatCard _facilities = new("Tổng cơ sở");
    private readonly TextBox _search;
    private readonly ComboBox _filter;

    public OwnersPage(AppServices app, IShell shell)
        : base(app, shell, "Chủ sân", "Theo dõi, khóa và mở khóa tài khoản chủ sân.", item => item.Id)
    {
        Grid.EmptyText = "Không tìm thấy chủ sân nào.";
        Grid.Add("Mã", item => $"#{item.Id}", 50, sort: item => item.Id)
            .Add("Chủ sân", item => item.FullName, 160, bold: true)
            .Add("Email", item => item.Email, 180)
            .Add("Số điện thoại", item => string.IsNullOrWhiteSpace(item.PhoneNumber) ? "Chưa cập nhật" : item.PhoneNumber, 110)
            .Add("Cơ sở", item => item.FacilityCount.ToString(), 60, right: true, sort: item => item.FacilityCount)
            .Add("Sân", item => item.CourtCount.ToString(), 60, right: true, sort: item => item.CourtCount)
            .Add("Ngày đăng ký", item => Display.DateFromUtc(item.CreatedAt), 100, sort: item => item.CreatedAt)
            .Add("Trạng thái", item => item.IsActive ? "Hoạt động" : "Đã khóa", 100,
                color: item => item.IsActive ? Theme.Success : Theme.Danger);

        _search = Tool(Ui.SearchBox("Tìm theo tên, email, số điện thoại", ReloadAsync));
        _filter = FilterCombo(new[]
        {
            new Option<string>("all", "Tất cả"),
            new Option<string>("active", "Đang hoạt động"),
            new Option<string>("locked", "Đang bị khóa")
        }, "all", 150);
        AddCommand("Tìm kiếm", ReloadAsync);
        AddAction("Khóa tài khoản",
            item => ToggleAsync(item, $"Bạn có chắc muốn KHÓA tài khoản của chủ sân {item.FullName}? "
                + "Khi bị khóa, chủ sân sẽ không thể đăng nhập."),
            item => item.IsActive, danger: true);
        AddAction("Mở khóa",
            item => ToggleAsync(item, $"Bạn có chắc muốn MỞ KHÓA tài khoản cho chủ sân {item.FullName}?"),
            item => !item.IsActive);

        Body.Controls.Add(GridCard(Grid));
        Body.Controls.Add(Gap());
        Body.Controls.Add(StatCard.Row(_total, _active, _locked, _facilities));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var search = _search.Text;
        var filter = Ui.Value<string>(_filter);
        var (stats, owners) = await App.CallAsync(async (IUserService service) =>
            (await service.GetCourtOwnerStatsAsync(), await service.GetCourtOwnersAsync(search, filter)));
        if (!isCurrent())
        {
            return;
        }

        _total.Set(stats.TotalOwners.ToString());
        _active.Set(stats.ActiveOwners.ToString());
        _locked.Set(stats.LockedOwners.ToString());
        _facilities.Set(stats.TotalFacilities.ToString());
        Grid.SetItems(owners);
    }

    private Task ToggleAsync(CourtOwnerItemDto owner, string question) => ExecuteAsync(question, async () =>
    {
        var result = await App.CallAsync((IUserService service) => service.ToggleLockOwnerAsync(owner.Id, App.UserId));
        return (result.Success, result.Message);
    });
}
