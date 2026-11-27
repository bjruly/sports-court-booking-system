using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Desktop.Dialogs;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class AdminFacilitiesPage : ListPage<FacilityItemDto>
{
    private readonly StatCard _total = new("Tổng cơ sở");
    private readonly StatCard _active = new("Đang hoạt động");
    private readonly StatCard _pending = new("Chờ duyệt");
    private readonly StatCard _suspended = new("Tạm khóa");
    private readonly TextBox _search;
    private readonly ComboBox _filter;

    public AdminFacilitiesPage(AppServices app, IShell shell)
        : base(app, shell, "Cơ sở thể thao", "Duyệt, khóa và cập nhật cơ sở thể thao.", item => item.Id)
    {
        Grid.EmptyText = "Không có cơ sở nào phù hợp điều kiện lọc.";
        Grid.Add("Tên cơ sở", item => item.Name, 170, bold: true)
            .Add("Môn", item => string.IsNullOrWhiteSpace(item.SportName) ? "Chưa có môn" : item.SportName, 90)
            .Add("Chủ sân", item => string.IsNullOrWhiteSpace(item.OwnerName)
                ? "—"
                : $"{item.OwnerName} {(string.IsNullOrWhiteSpace(item.OwnerPhone) ? "" : $"({item.OwnerPhone})")}", 130)
            .Add("Địa chỉ", item => item.Address, 190)
            .Add("Hotline", item => item.PhoneNumber, 95)
            .Add("Giờ hoạt động", item => $"{item.OpenTime} - {item.CloseTime}", 95)
            .Add("Sân", item => item.TotalCourts.ToString(), 50, right: true, sort: item => item.TotalCourts)
            .Add("Trạng thái", StatusText, 130, color: item =>
                !item.IsActive ? Theme.Danger : item.IsApproved ? Theme.Success : Theme.Warning);

        _search = Tool(Ui.SearchBox("Tìm theo tên, địa chỉ, hotline", ReloadAsync, 220));
        _filter = FilterCombo(new[]
        {
            new Option<string>("all", "Tất cả"),
            new Option<string>("active", "Đang hoạt động"),
            new Option<string>("pending", "Chờ duyệt"),
            new Option<string>("suspended", "Tạm khóa")
        }, "all", 150);
        AddCommand("Tìm kiếm", ReloadAsync);
        AddCommand("+ Thêm cơ sở", () => EditAsync(null), primary: true);
        AddAction("Duyệt", item => ExecuteAsync(
            $"Xác nhận phê duyệt cơ sở \"{item.Name}\" hoạt động chính thức?",
            () => App.CallAsync((IFacilityService service) => service.ApproveFacilityAsync(item.Id, App.UserId))),
            item => !item.IsApproved);
        AddAction("Tạm khóa", LockAsync, item => item.IsActive, danger: true);
        AddAction("Mở khóa", item => ExecuteAsync(
            $"Mở khóa cho cơ sở \"{item.Name}\" hoạt động trở lại?",
            () => ToggleLockAsync(item.Id, null)),
            item => !item.IsActive);
        AddAction("Sửa", EditAsync);

        Body.Controls.Add(GridCard(Grid));
        Body.Controls.Add(Gap());
        Body.Controls.Add(StatCard.Row(_total, _active, _pending, _suspended));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var search = _search.Text;
        var filter = Ui.Value<string>(_filter);
        var (stats, facilities) = await App.CallAsync(async (IFacilityService service) =>
            (await service.GetFacilityStatsAsync(), await service.GetAllFacilitiesAsync(search, filter)));
        if (!isCurrent())
        {
            return;
        }

        _total.Set(stats.TotalFacilities.ToString());
        _active.Set(stats.ActiveFacilities.ToString());
        _pending.Set(stats.PendingApprovalFacilities.ToString());
        _suspended.Set(stats.SuspendedFacilities.ToString());
        Grid.SetItems(facilities);
    }

    private static string StatusText(FacilityItemDto facility)
    {
        if (!facility.IsActive)
        {
            return string.IsNullOrWhiteSpace(facility.SuspensionReason)
                ? "Tạm khóa"
                : $"Tạm khóa: {facility.SuspensionReason}";
        }

        return facility.IsApproved ? "Đang hoạt động" : "Chờ duyệt";
    }

    private Task LockAsync(FacilityItemDto facility) => PromptAsync(
        $"Tạm khóa cơ sở {facility.Name}",
        "Lý do tạm khóa",
        "Tạm khóa",
        reason => ToggleLockAsync(facility.Id, reason),
        required: false,
        note: "Các sân thuộc cơ sở sẽ ngừng nhận đặt cho đến khi mở khóa.");

    private async Task<(bool Success, string Message)> ToggleLockAsync(int facilityId, string? reason)
    {
        var result = await App.CallAsync((IFacilityService service) => service.ToggleLockFacilityAsync(facilityId, reason, App.UserId));
        return (result.Success, result.Message);
    }

    private Task EditAsync(FacilityItemDto? facility)
    {
        var dialog = new FormDialog(facility is null ? "Thêm cơ sở thể thao" : "Cập nhật cơ sở thể thao");
        var name = dialog.AddText("Tên cơ sở", facility?.Name, 150);
        var address = dialog.AddText("Địa chỉ", facility?.Address);
        var phone = dialog.AddText("Số điện thoại", facility?.PhoneNumber, 20);
        var open = dialog.AddTime("Giờ mở cửa", facility?.OpenTime ?? "06:00");
        var close = dialog.AddTime("Giờ đóng cửa", facility?.CloseTime ?? "22:00");
        var description = dialog.AddText("Mô tả", facility?.Description, 1000, 4);
        dialog.OnSubmit(async () =>
        {
            if (facility is null)
            {
                var create = new CreateFacilityRequest
                {
                    Name = name.Text,
                    Address = address.Text,
                    PhoneNumber = phone.Text,
                    OpenTime = FormDialog.TimeText(open),
                    CloseTime = FormDialog.TimeText(close),
                    Description = description.Text
                };
                var created = await App.CallAsync((IFacilityService service) => service.CreateFacilityAsync(create, App.UserId));
                return (created.Success, created.Message);
            }

            var update = new UpdateFacilityRequest
            {
                Id = facility.Id,
                Name = name.Text,
                Address = address.Text,
                PhoneNumber = phone.Text,
                OpenTime = FormDialog.TimeText(open),
                CloseTime = FormDialog.TimeText(close),
                Description = description.Text
            };
            return await App.CallAsync((IFacilityService service) => service.UpdateFacilityAsync(update, App.UserId));
        });
        return ShowDialogAsync(dialog);
    }
}
