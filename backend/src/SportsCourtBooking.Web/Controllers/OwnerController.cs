using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SportsCourtBooking.Web.Controllers;

[Authorize(Roles = "CourtOwner")]
public class OwnerController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
