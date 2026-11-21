namespace SportsCourtBooking.Application.DTOs;

public class OwnerDashboardDto
{
    public int TotalCourts { get; set; }
    public int ActiveCourts { get; set; }
    public int MaintenanceCourts { get; set; }
    public int InactiveCourts { get; set; }
    public int TodayBookings { get; set; }
    public int PendingBookings { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<BookingDto> RecentBookings { get; set; } = new();
}
