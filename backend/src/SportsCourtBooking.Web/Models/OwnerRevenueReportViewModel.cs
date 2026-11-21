using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class OwnerRevenueReportViewModel
{
    public List<CourtDto> Courts { get; set; } = new();
    public OwnerRevenueReportDto Report { get; set; } = new();
    public int SelectedYear { get; set; }
    public int SelectedMonth { get; set; }
    public int? SelectedCourtId { get; set; }
}
