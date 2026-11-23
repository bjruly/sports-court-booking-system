using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class CategoryReportPage : PageBase
{
    private readonly DataGrid<CategoryDetailReportDto> _categories;
    private readonly DataGrid<CategoryCourtItemDto> _courts;

    public CategoryReportPage(AppServices app, IShell shell)
        : base(app, shell, "Báo cáo danh mục", "Chọn một môn để xem chi tiết từng sân bên dưới.")
    {
        _categories = new DataGrid<CategoryDetailReportDto>(item => item.CourtTypeId)
            .Add("Môn / Loại sân", item => item.CourtTypeName, 140, bold: true)
            .Add("Mô tả", item => item.Description, 190)
            .Add("Hoạt động / tổng sân", item => $"{item.ActiveCourts} / {item.TotalCourts}", 100, right: true, sort: item => item.ActiveCourts)
            .Add("Giá / giờ", item => item.TotalCourts == 0
                ? "—"
                : $"{Display.Money(item.MinPricePerHour)} – {Display.Money(item.MaxPricePerHour)}", 190, right: true, sort: item => item.MinPricePerHour)
            .Add("Tổng đơn", item => item.TotalBookings.ToString(), 70, right: true, sort: item => item.TotalBookings)
            .Add("Thành công", item => item.SuccessfulBookings.ToString(), 80, right: true, sort: item => item.SuccessfulBookings)
            .Add("Tỷ lệ", item => Display.Percent(item.SuccessRate), 70, right: true, sort: item => item.SuccessRate)
            .Add("Doanh thu", item => Display.Money(item.Revenue), 110, right: true, sort: item => item.Revenue)
            .Add("Tỷ trọng", item => Display.Percent(item.ContributionPercent), 70, right: true, sort: item => item.ContributionPercent);
        _courts = new DataGrid<CategoryCourtItemDto>(item => item.Id) { EmptyText = "Chọn một môn có sân để xem chi tiết." }
            .Add("Sân", item => item.Name, 170, bold: true)
            .Add("Địa điểm", item => item.Location, 220)
            .Add("Giá / giờ", item => Display.Money(item.PricePerHour), 100, right: true, sort: item => item.PricePerHour)
            .Add("Trạng thái", item => Display.Label(item.Status), 110, color: item => Display.Tone(item.Status))
            .Add("Tổng đơn", item => item.TotalBookings.ToString(), 80, right: true, sort: item => item.TotalBookings)
            .Add("Doanh thu", item => Display.Money(item.Revenue), 110, right: true, sort: item => item.Revenue);
        _categories.SelectionChanged += (_, _) =>
            _courts.SetItems(_categories.Selected?.Courts ?? new List<CategoryCourtItemDto>());

        Tool(Ui.Button("In báo cáo", (_, _) => new GridPrinter(
            "Báo cáo danh mục", Array.Empty<string>(), ("Theo môn / loại sân", _categories)).Preview(this)));
        Body.Controls.Add(Stack(
            GridCard(_categories, "Theo môn / loại sân"),
            GridCard(_courts, "Chi tiết từng sân"),
            55));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var reports = await App.CallAsync((IReportService service) => service.GetCategoryReportsAsync());
        if (isCurrent())
        {
            _categories.SetItems(reports);
        }
    }
}
