using Microsoft.AspNetCore.Mvc.Rendering;
using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class OwnerCourtFormViewModel : OwnerCourtRequest
{
    public List<SelectListItem> CourtTypes { get; set; } = new();
    public List<SelectListItem> Facilities { get; set; } = new();
}
