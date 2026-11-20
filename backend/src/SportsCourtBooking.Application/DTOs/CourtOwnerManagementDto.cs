namespace SportsCourtBooking.Application.DTOs;

public class CourtOwnerItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int FacilityCount { get; set; }
    public int CourtCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourtOwnerStatsDto
{
    public int TotalOwners { get; set; }
    public int ActiveOwners { get; set; }
    public int LockedOwners { get; set; }
    public int TotalFacilities { get; set; }
}
