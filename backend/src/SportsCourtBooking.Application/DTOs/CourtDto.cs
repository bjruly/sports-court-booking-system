using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class CourtDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public int CourtTypeId { get; set; }
    public string CourtTypeName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal PricePerHour { get; set; }
    public int PeakStartHour { get; set; } = 17;
    public decimal PeakSurcharge { get; set; } = 50_000m;
    public CourtStatus Status { get; set; }
}