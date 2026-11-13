namespace SportsCourtBooking.Domain.Entities;

public class CourtType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<Court> Courts { get; set; } = new List<Court>();
}