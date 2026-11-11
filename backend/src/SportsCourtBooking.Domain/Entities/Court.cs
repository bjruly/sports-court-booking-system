using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Domain.Entities;

public class Court : BaseEntity
{
    public string Name { get; set; } = string.Empty; // VD: Sân Cầu Lông Số 1
    public int CourtTypeId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal PricePerHour { get; set; }
    public CourtStatus Status { get; set; } = CourtStatus.Active;

    // Navigation properties
    public CourtType? CourtType { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}