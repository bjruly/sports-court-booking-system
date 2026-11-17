using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Extensions;

namespace SportsCourtBooking.Web.Controllers;

[Authorize(Roles = "CourtOwner")]
public class OwnerController : Controller
{
    private readonly IOwnerDashboardService _dashboardService;

    public OwnerController(IOwnerDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var dashboard = await _dashboardService.GetDashboardAsync(ownerId.Value);
        return View(dashboard);
    }
}
