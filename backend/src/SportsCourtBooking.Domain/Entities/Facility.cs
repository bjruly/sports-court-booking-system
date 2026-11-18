namespace SportsCourtBooking.Domain.Entities;

public class Facility : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "22:00";
    public string? ImageUrl { get; set; }
    public int? OwnerId { get; set; }
    public bool IsApproved { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? SuspensionReason { get; set; }

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
