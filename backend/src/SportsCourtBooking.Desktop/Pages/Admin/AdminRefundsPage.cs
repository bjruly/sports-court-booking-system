using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

/// <summary>Admin chỉ theo dõi; việc hoàn tiền do chủ sân xử lý.</summary>
public sealed class AdminRefundsPage : ListPage<RefundDto>
{
    public AdminRefundsPage(AppServices app, IShell shell)
        : base(app, shell, "Theo dõi hoàn tiền", "Yêu cầu hoàn tiền trên toàn hệ thống, do chủ sân xử lý.", item => item.Id)
    {
        Grid.EmptyText = "Chưa có yêu cầu hoàn tiền nào.";
        Grid.Add("Đơn", item => $"#{item.BookingId}", 50, sort: item => item.BookingId)
            .Add("Khách", item => item.CustomerName, 130, bold: true)
            .Add("Sân", item => item.CourtName, 150)
            .Add("Ngày chơi", item => Display.Date(item.BookingDate), 90, sort: item => item.BookingDate)
            .Add("Số tiền", item => Display.Money(item.Amount), 100, right: true, sort: item => item.Amount)
            .Add("Lý do", item => item.Reason, 200)
            .Add("Yêu cầu lúc", item => Display.DateTimeFromUtc(item.RequestedAt), 120, sort: item => item.RequestedAt)
            .Add("Trạng thái", item => Display.Label(item.Status), 120, color: item => Display.Tone(item.Status))
            .Add("Giao dịch", item => item.TransactionCode ?? item.RejectionReason ?? "—", 130);
        Body.Controls.Add(GridCard(Grid));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var refunds = await App.CallAsync((IPaymentService service) => service.GetAdminRefundsAsync());
        if (isCurrent())
        {
            Grid.SetItems(refunds);
        }
    }
}
