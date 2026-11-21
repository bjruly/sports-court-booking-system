using System.ComponentModel.DataAnnotations;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Domain.Entities;

public class Refund : BaseEntity
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public DateTime RequestedAt { get; set; }
    public int? RequestedByUserId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int? ProcessedByUserId { get; set; }
    public string? TransactionCode { get; set; }
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public Payment? Payment { get; set; }
    public User? RequestedByUser { get; set; }
    public User? ProcessedByUser { get; set; }
}
