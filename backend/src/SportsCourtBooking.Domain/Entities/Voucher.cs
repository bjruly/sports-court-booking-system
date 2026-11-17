using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Domain.Entities;

public class Voucher : BaseEntity
{
    public int OwnerId { get; set; }
    public string Code { get; set; } = string.Empty;
    public VoucherDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;

    public User? Owner { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
