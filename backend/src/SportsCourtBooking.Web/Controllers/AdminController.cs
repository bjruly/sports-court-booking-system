using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Extensions;

namespace SportsCourtBooking.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;

    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // ─── Quản lý khách hàng ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Customers(string? search)
    {
        ViewData["Search"] = search;
        var customers = await _userService.GetCustomersAsync(search);
        return View(customers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int userId, string? returnSearch)
    {
        var adminId = User.GetUserId() ?? 0;
        var (success, message, isActive) = await _userService.ToggleLockAsync(userId, adminId);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Customers), new { search = returnSearch });
    }
}
