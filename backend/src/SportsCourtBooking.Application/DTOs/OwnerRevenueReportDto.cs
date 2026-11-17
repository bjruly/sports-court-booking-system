namespace SportsCourtBooking.Application.DTOs;

public class OwnerRevenueReportDto
{
    public DateOnly MonthStart { get; set; }
    public int TotalBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int ExpiredBookings { get; set; }
    public decimal BookingValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal CollectedForCancelledBookings { get; set; }
    public List<OwnerCourtRevenueDto> CourtReports { get; set; } = new();
    public List<OwnerDailyRevenueDto> DailyReports { get; set; } = new();
}

public class OwnerCourtRevenueDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int SuccessfulBookings { get; set; }
    public decimal BookingValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal CollectedForCancelledBookings { get; set; }
}

public class OwnerDailyRevenueDto
{
    public DateOnly Date { get; set; }
    public int TotalBookings { get; set; }
    public int SuccessfulBookings { get; set; }
    public decimal BookingValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal CollectedForCancelledBookings { get; set; }
}
