namespace SportsCourtBooking.Domain.Entities;

public class Facility : BaseEntity
{
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }

    public User? Owner { get; set; }
    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
