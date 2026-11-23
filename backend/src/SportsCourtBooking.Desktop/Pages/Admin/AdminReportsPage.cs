using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class AdminReportsPage : PageBase
{
    private readonly StatCard _revenue = new("Doanh thu ghi nhận");
    private readonly StatCard _bookings = new("Tổng đơn đặt");
    private readonly StatCard _customers = new("Khách hàng");
    private readonly StatCard _courts = new("Sân hoạt động / tổng");
    private readonly DataGrid<CourtTypeReportDto> _sports = ReportGrids.Sports();
    private readonly DataGrid<BookingStatusReportDto> _statuses;
    private AdminReportDto? _report;

    public AdminReportsPage(AppServices app, IShell shell)
        : base(app, shell, "Báo cáo tổng hợp", "Doanh thu và đơn đặt của toàn hệ thống.")
    {
        _statuses = new DataGrid<BookingStatusReportDto>(item => item.Status)
            .Add("Trạng thái đơn", item => item.StatusName, 160, bold: true, color: item => Display.Tone(item.Status))
            .Add("Số lượng đơn", item => item.Count.ToString(), 100, right: true, sort: item => item.Count)
            .Add("Tỷ lệ", item => Display.Percent(item.Percent), 80, right: true, sort: item => item.Percent)
            .Add("Giá trị tương ứng", item => Display.Money(item.TotalAmount), 140, right: true, sort: item => item.TotalAmount);

        Tool(Ui.Button("In báo cáo", (_, _) => Print()));
        Body.Controls.Add(Stack(
            GridCard(_sports, "Theo môn / loại sân"),
            GridCard(_statuses, "Theo trạng thái đơn"),
            50));
        Body.Controls.Add(Gap());
        Body.Controls.Add(StatCard.Row(_revenue, _bookings, _customers, _courts));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var report = await App.CallAsync((IReportService service) => service.GetAdminReportAsync());
        if (!isCurrent())
        {
            return;
        }

        _report = report;
        _revenue.Set(Display.Money(report.TotalRevenue), $"Tạm tính chờ duyệt: {Display.Money(report.PendingRevenue)}");
        _bookings.Set(report.TotalBookings.ToString(),
            $"Thành công: {report.ConfirmedBookings} · Đã hủy: {report.CancelledBookings}");
        _customers.Set(report.TotalCustomers.ToString(),
            $"Hoạt động: {report.ActiveCustomers} · Khóa: {report.LockedCustomers}");
        _courts.Set($"{report.ActiveCourts} / {report.TotalCourts}",
            $"Tỷ lệ khả dụng: {Display.Percent(report.TotalCourts == 0 ? 0 : (double)report.ActiveCourts / report.TotalCourts * 100)}");
        _sports.SetItems(report.CourtTypeReports);
        _statuses.SetItems(report.BookingStatusReports);
    }

    private void Print()
    {
        if (_report is null)
        {
            return;
        }

        new GridPrinter(
            "Báo cáo tổng hợp",
            new[]
            {
                $"Doanh thu ghi nhận: {Display.Money(_report.TotalRevenue)} · Chờ duyệt: {Display.Money(_report.PendingRevenue)}",
                $"Tổng đơn: {_report.TotalBookings} · Khách hàng: {_report.TotalCustomers} · Sân hoạt động: {_report.ActiveCourts}/{_report.TotalCourts}"
            },
            ("Theo môn / loại sân", _sports),
            ("Theo trạng thái đơn", _statuses)).Preview(this);
    }
}
