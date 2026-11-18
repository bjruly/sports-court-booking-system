using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Extensions;

namespace SportsCourtBooking.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly IReportService _reportService;
    private readonly ICategoryService _categoryService;

    public AdminController(
        IUserService userService,
        IReportService reportService,
        ICategoryService categoryService)
    {
        _userService = userService;
        _reportService = reportService;
        _categoryService = categoryService;
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

    [HttpGet]
    public async Task<IActionResult> CategoryReport()
    {
        var categoryReports = await _reportService.GetCategoryReportsAsync();
        return View(categoryReports);
    }

    // ─── Quản lý danh mục ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Categories(string? search)
    {
        ViewData["Search"] = search;
        var categories = await _categoryService.GetAllCategoriesAsync(search);
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin danh mục không hợp lệ.";
            return RedirectToAction(nameof(Categories));
        }

        var (success, message, _) = await _categoryService.CreateCategoryAsync(request);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin cập nhật không hợp lệ.";
            return RedirectToAction(nameof(Categories));
        }

        var (success, message) = await _categoryService.UpdateCategoryAsync(request);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var (success, message) = await _categoryService.DeleteCategoryAsync(id);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Categories));
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
