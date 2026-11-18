namespace SportsCourtBooking.Application.DTOs;

public class FacilityItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "22:00";
    public string? ImageUrl { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public string? SuspensionReason { get; set; }
    public int TotalCourts { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFacilityRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "22:00";
}

public class UpdateFacilityRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "22:00";
}

public class FacilityStatsDto
{
    public int TotalFacilities { get; set; }
    public int ActiveFacilities { get; set; }
    public int PendingApprovalFacilities { get; set; }
    public int SuspendedFacilities { get; set; }
}
