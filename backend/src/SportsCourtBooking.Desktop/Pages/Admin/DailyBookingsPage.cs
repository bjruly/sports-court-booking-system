using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

/// <summary>Lịch đặt của mọi sân theo ngày, dành cho quản trị viên.</summary>
public sealed class DailyBookingsPage : ListPage<BookingDto>
{
    private readonly DateTimePicker _date;
    private readonly ComboBox _court;

    public DailyBookingsPage(AppServices app, IShell shell)
        : base(app, shell, "Lịch đặt theo ngày", "Xác nhận hoặc hủy đơn của mọi sân trong ngày.", item => item.Id)
    {
        Grid.EmptyText = "Chưa có đơn nào cho ngày và sân đã chọn.";
        Grid.Add("Mã đơn", item => $"#{item.Id}", 60, sort: item => item.Id)
            .Add("Sân", item => item.CourtName, 180, bold: true)
            .Add("Khách hàng", item => item.CustomerName, 150)
            .Add("Điện thoại", item => item.CustomerPhone, 100)
            .Add("Giờ", item => Display.TimeRange(item.StartTime, item.EndTime), 100, sort: item => item.StartTime)
            .Add("Tổng tiền", item => Display.Money(item.TotalPrice), 110, right: true, sort: item => item.TotalPrice)
            .Add("Thanh toán", item => Display.Label(item.PaymentStatus), 120)
            .Add("Trạng thái", item => Display.Label(item.Status), 110, color: item => Display.Tone(item.Status));

        _date = Tool(Ui.DatePicker(VietnamClock.Today));
        _date.ValueChanged += async (_, _) => await ReloadAsync();
        _court = FilterCombo(new[] { new Option<int?>(null, "Tất cả sân") }, width: 240);
        AddAction("Xác nhận", item => ExecuteAsync(null,
            () => App.CallAsync((IBookingService service) => service.ConfirmBookingAsync(item.Id, App.UserId))),
            item => BookingRules.CanConfirm(item.Status) && item.IsPaid, primary: true);
        AddAction("Hủy đơn", item => ExecuteAsync($"Bạn chắc chắn muốn hủy đơn #{item.Id}?",
            () => App.CallAsync((IBookingService service) => service.CancelBookingAsync(item.Id, App.UserId))),
            item => BookingRules.CanCancel(item.Status, item.BookingDate, item.StartTime, VietnamClock.Now, item.CheckInAt),
            danger: true);
        Body.Controls.Add(GridCard(Grid));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var date = Ui.DateOf(_date);
        var courtId = Ui.Value<int?>(_court);
        var courts = await App.CallAsync((ICourtService service) => service.GetAllCourtsAsync());
        var bookings = await App.CallAsync((IBookingService service) =>
            service.GetBookingsByDateAsync(date, courtId, includeInactive: true));
        if (!isCurrent())
        {
            return;
        }

        Ui.SetOptions(_court, courts
            .Select(court => new Option<int?>(court.Id, court.Name))
            .Prepend(new Option<int?>(null, "Tất cả sân")));
        Grid.SetItems(bookings);
    }
}
