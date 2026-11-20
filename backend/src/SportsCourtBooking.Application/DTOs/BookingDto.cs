using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class CreateBookingRequest
{
    public int UserId { get; set; }
    public int CourtId { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Note { get; set; }
    public string? VoucherCode { get; set; }
}

public class OwnerWalkInBookingRequest
{
    public int CourtId { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class BookingDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime? CheckInAt { get; set; }
    public string? VoucherCode { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class BookingDetailDto : BookingDto
{
    public string Location { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsPaid { get; set; }

    public PaymentStatus? PaymentStatus { get; set; }

    public string? PaymentMethod { get; set; }

    public RefundStatus? RefundStatus { get; set; }

    public decimal? RefundAmount { get; set; }

    public bool CanReview { get; set; }

    public int? ReviewId { get; set; }

    public int? ReviewRating { get; set; }

    public string? ReviewComment { get; set; }
}
