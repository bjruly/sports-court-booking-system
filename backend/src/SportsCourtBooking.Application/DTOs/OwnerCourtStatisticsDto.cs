using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class OwnerCourtStatisticsDto
{
    public DateOnly MonthStart { get; set; }
    public int TotalCourts { get; set; }
    public int ActiveCourts { get; set; }
    public int TotalBookings { get; set; }
    public int SuccessfulBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int ExpiredBookings { get; set; }
    public List<OwnerCourtStatisticsRowDto> Courts { get; set; } = new();
}

public class OwnerCourtStatisticsRowDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public CourtStatus Status { get; set; }
    public int TotalBookings { get; set; }
    public int SuccessfulBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int ExpiredBookings { get; set; }
    public double SuccessRatePercent => TotalBookings == 0
        ? 0
        : (double)SuccessfulBookings / TotalBookings * 100;
}
