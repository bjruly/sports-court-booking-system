using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

public class OwnerCourtsViewModel
{
    public string? Search { get; set; }
    public int? SelectedFacilityId { get; set; }
    public List<OwnerFacilityGroupViewModel> Facilities { get; set; } = new();
    public List<CourtDto> UnassignedCourts { get; set; } = new();

    public int TotalFacilities => Facilities.Count;
    public int TotalCourts => Facilities.Sum(f => f.Courts.Count) + UnassignedCourts.Count;
    public int TotalActiveCourts => Facilities.Sum(f => f.Courts.Count(c => c.Status == CourtStatus.Active))
        + UnassignedCourts.Count(c => c.Status == CourtStatus.Active);
    public int TotalMaintenanceCourts => Facilities.Sum(f => f.Courts.Count(c => c.Status == CourtStatus.Maintenance))
        + UnassignedCourts.Count(c => c.Status == CourtStatus.Maintenance);
    public int TotalInactiveCourts => Facilities.Sum(f => f.Courts.Count(c => c.Status == CourtStatus.Inactive))
        + UnassignedCourts.Count(c => c.Status == CourtStatus.Inactive);
}

public class OwnerFacilityGroupViewModel
{
    public OwnerFacilityDto Facility { get; set; } = default!;
    public List<CourtDto> Courts { get; set; } = new();
    public bool IsExpanded { get; set; }
}
