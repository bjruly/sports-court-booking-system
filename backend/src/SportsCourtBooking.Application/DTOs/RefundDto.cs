using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class RefundDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int PaymentId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? TransactionCode { get; set; }
    public string? RejectionReason { get; set; }
}
