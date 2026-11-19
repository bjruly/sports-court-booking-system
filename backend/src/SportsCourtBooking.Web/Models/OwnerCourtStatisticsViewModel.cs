using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class OwnerCourtStatisticsViewModel
{
    public OwnerCourtStatisticsDto Statistics { get; set; } = new();
    public int SelectedYear { get; set; }
    public int SelectedMonth { get; set; }
}
