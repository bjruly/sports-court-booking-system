using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Extensions;

namespace SportsCourtBooking.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly IReportService _reportService;

    public AdminController(IUserService userService, IReportService reportService)
    {
        _userService = userService;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var report = await _reportService.GetAdminReportAsync();
        return View(report);
    }

    // ─── Báo cáo thống kê ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Reports()
    {
        var report = await _reportService.GetAdminReportAsync();
        return View(report);
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
