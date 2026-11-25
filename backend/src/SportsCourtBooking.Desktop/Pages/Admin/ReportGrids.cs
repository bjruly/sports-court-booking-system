using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

internal static class ReportGrids
{
    public static DataGrid<CourtTypeReportDto> Sports()
        => new DataGrid<CourtTypeReportDto>(item => item.CourtTypeId) { EmptyText = "Chưa có dữ liệu loại sân." }
            .Add("Môn / Loại sân", item => item.CourtTypeName, 150, bold: true)
            .Add("Số sân", item => item.CourtCount.ToString(), 70, right: true, sort: item => item.CourtCount)
            .Add("Tổng đơn", item => item.TotalBookings.ToString(), 80, right: true, sort: item => item.TotalBookings)
            .Add("Thành công", item => item.SuccessfulBookings.ToString(), 90, right: true, sort: item => item.SuccessfulBookings)
            .Add("Doanh thu", item => Display.Money(item.Revenue), 120, right: true, sort: item => item.Revenue)
            .Add("Tỷ trọng", item => Display.Percent(item.ContributionPercent), 80, right: true, sort: item => item.ContributionPercent);
}
