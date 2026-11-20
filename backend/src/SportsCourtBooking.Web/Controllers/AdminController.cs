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
    private readonly IReviewService _reviewService;
    private readonly IFacilityService _facilityService;
    private readonly IPaymentService _paymentService;

    public AdminController(
        IUserService userService,
        IReportService reportService,
        ICategoryService categoryService,
        IReviewService reviewService,
        IFacilityService facilityService,
        IPaymentService paymentService)
    {
        _userService = userService;
        _reportService = reportService;
        _categoryService = categoryService;
        _reviewService = reviewService;
        _facilityService = facilityService;
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var report = await _reportService.GetAdminReportAsync();
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Refunds()
        => View(await _paymentService.GetAdminRefundsAsync());

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

    // ─── Quản lý chủ sân (Owner) ─────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Owners(string? search, string? filter)
    {
        ViewData["Search"] = search;
        ViewData["Filter"] = filter ?? "all";
        ViewBag.Stats = await _userService.GetCourtOwnerStatsAsync();

        var owners = await _userService.GetCourtOwnersAsync(search, filter);
        return View(owners);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockOwner(int id, string? returnSearch, string? returnFilter)
    {
        var adminId = User.GetUserId() ?? 0;
        var (success, message, _) = await _userService.ToggleLockOwnerAsync(id, adminId);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Owners), new { search = returnSearch, filter = returnFilter });
    }

    // ─── UC26: Quản lý & Xử lý đánh giá vi phạm ──────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Reviews(string? search, string? filter)
    {
        ViewData["Search"] = search;
        ViewData["Filter"] = filter ?? "all";
        
        var reviews = await _reviewService.GetAllReviewsAsync(search, filter);
        var stats = await _reviewService.GetReviewStatsAsync();
        ViewData["Stats"] = stats;

        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideReview(int id, string reason, string? returnSearch, string? returnFilter)
    {
        var (success, message) = await _reviewService.HideReviewAsync(id, reason);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Reviews), new { search = returnSearch, filter = returnFilter });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnhideReview(int id, string? returnSearch, string? returnFilter)
    {
        var (success, message) = await _reviewService.UnhideReviewAsync(id);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Reviews), new { search = returnSearch, filter = returnFilter });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(int id, string? returnSearch, string? returnFilter)
    {
        var (success, message) = await _reviewService.DeleteReviewAsync(id);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Reviews), new { search = returnSearch, filter = returnFilter });
    }

    // ─── Quản lý cơ sở thể thao (UC13, UC23) ─────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Facilities(string? search, string? filter)
    {
        ViewData["Search"] = search;
        ViewData["Filter"] = filter ?? "all";
        ViewBag.Stats = await _facilityService.GetFacilityStatsAsync();
        var facilities = await _facilityService.GetAllFacilitiesAsync(search, filter);
        return View(facilities);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFacility(CreateFacilityRequest request)
    {
        var (success, message, _) = await _facilityService.CreateFacilityAsync(request);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Facilities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFacility(UpdateFacilityRequest request)
    {
        var (success, message) = await _facilityService.UpdateFacilityAsync(request);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Facilities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveFacility(int id, string? returnSearch, string? returnFilter)
    {
        var (success, message) = await _facilityService.ApproveFacilityAsync(id);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Facilities), new { search = returnSearch, filter = returnFilter });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockFacility(int id, string? reason, string? returnSearch, string? returnFilter)
    {
        var (success, message, _) = await _facilityService.ToggleLockFacilityAsync(id, reason);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Facilities), new { search = returnSearch, filter = returnFilter });
    }
}
