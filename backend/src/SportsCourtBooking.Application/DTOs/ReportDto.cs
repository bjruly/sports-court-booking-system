using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class AdminReportDto
{
    // Doanh thu
    public decimal TotalRevenue { get; set; }           // Tổng doanh thu thực tế (Confirmed + Completed)
    public decimal PendingRevenue { get; set; }         // Doanh thu đang chờ duyệt (Pending)
    
    // Đơn đặt
    public int TotalBookings { get; set; }              // Tổng số đơn
    public int ConfirmedBookings { get; set; }          // Đơn đã duyệt / hoàn thành
    public int PendingBookings { get; set; }            // Đơn đang chờ
    public int CancelledBookings { get; set; }          // Đơn đã hủy / hết hạn
    
    // Khách hàng
    public int TotalCustomers { get; set; }             // Tổng số khách hàng
    public int ActiveCustomers { get; set; }            // Khách hàng đang hoạt động
    public int LockedCustomers { get; set; }            // Khách hàng bị khóa
    
    // Sân thể thao
    public int TotalCourts { get; set; }                // Tổng số sân
    public int ActiveCourts { get; set; }               // Sân đang hoạt động

    // Báo cáo tổng hợp theo môn / loại sân
    public List<CourtTypeReportDto> CourtTypeReports { get; set; } = new();

    // Báo cáo tổng hợp theo trạng thái đơn
    public List<BookingStatusReportDto> BookingStatusReports { get; set; } = new();

    // Doanh thu theo 7 ngày gần nhất (dữ liệu thật)
    public List<DailyRevenueDto> RecentDailyRevenues { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int BookingCount { get; set; }
}

public class CourtTypeReportDto
{
    public int CourtTypeId { get; set; }
    public string CourtTypeName { get; set; } = string.Empty;
    public int CourtCount { get; set; }
    public int TotalBookings { get; set; }
    public int SuccessfulBookings { get; set; }
    public decimal Revenue { get; set; }
    public double ContributionPercent { get; set; }
}

public class BookingStatusReportDto
{
    public BookingStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public double Percent { get; set; }
}

public class CategoryDetailReportDto
{
    public int CourtTypeId { get; set; }
    public string CourtTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalCourts { get; set; }
    public int ActiveCourts { get; set; }
    public decimal MinPricePerHour { get; set; }
    public decimal MaxPricePerHour { get; set; }
    
    public int TotalBookings { get; set; }
    public int SuccessfulBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CancelledBookings { get; set; }
    public double SuccessRate { get; set; }
    
    public decimal Revenue { get; set; }
    public decimal PendingRevenue { get; set; }
    public double ContributionPercent { get; set; }
    public decimal AverageOrderValue { get; set; }

    public List<CategoryCourtItemDto> Courts { get; set; } = new();
}

public class CategoryCourtItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal PricePerHour { get; set; }
    public CourtStatus Status { get; set; }
    public int TotalBookings { get; set; }
    public decimal Revenue { get; set; }
}
