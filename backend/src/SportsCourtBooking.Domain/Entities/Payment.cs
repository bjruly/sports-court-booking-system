using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Domain.Entities;

public class Payment : BaseEntity
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, VNPay, MoMo, BankTransfer
    public string? TransactionCode { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;
    public DateTime? PaidAt { get; set; }

    // Navigation property
    public Booking? Booking { get; set; }
}