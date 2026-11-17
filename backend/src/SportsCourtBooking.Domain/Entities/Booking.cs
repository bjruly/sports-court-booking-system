using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Domain.Entities;

public class Booking : BaseEntity
{
    public const int PendingExpirationMinutes = 10;

    public int? UserId { get; set; }
    public int CourtId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public DateTime? CheckInAt { get; set; }
    
    public DateOnly BookingDate { get; set; }      // Ngày đặt sân
    public TimeOnly StartTime { get; set; }        // Giờ bắt đầu (VD: 17:00)
    public TimeOnly EndTime { get; set; }          // Giờ kết thúc (VD: 19:00)
    
    public decimal TotalPrice { get; set; }
    public int? VoucherId { get; set; }
    public string? VoucherCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Note { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Court? Court { get; set; }
    public Payment? Payment { get; set; }
    public Voucher? Voucher { get; set; }
}