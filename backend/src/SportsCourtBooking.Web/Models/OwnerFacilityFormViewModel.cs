using Microsoft.AspNetCore.Mvc.Rendering;
using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class OwnerFacilityFormViewModel : OwnerFacilityRequest
{
    public List<SelectListItem> CourtTypes { get; set; } = new();
    public List<SelectListItem> DistrictList { get; set; } = new();
}
