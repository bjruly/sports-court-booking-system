using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Domain.Entities;

public class Booking : BaseEntity
{
    public int UserId { get; set; }
    public int CourtId { get; set; }
    
    public DateOnly BookingDate { get; set; }      // Ngày đặt sân
    public TimeOnly StartTime { get; set; }        // Giờ bắt đầu (VD: 17:00)
    public TimeOnly EndTime { get; set; }          // Giờ kết thúc (VD: 19:00)
    
    public decimal TotalPrice { get; set; }
    public string? Note { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    // Navigation properties
    public User? User { get; set; }
    public Court? Court { get; set; }
    public Payment? Payment { get; set; }
}