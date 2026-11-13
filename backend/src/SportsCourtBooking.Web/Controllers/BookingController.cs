using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Extensions;
using SportsCourtBooking.Web.Models;

namespace SportsCourtBooking.Web.Controllers;

public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly ICourtService _courtService;

    public BookingController(IBookingService bookingService, ICourtService courtService)
    {
        _bookingService = bookingService;
        _courtService = courtService;
    }

    // Xem lịch đặt của tất cả khách theo ngày và sân (chỉ nhân viên / quản trị viên, vì có tên khách hàng)
    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, int? courtId, int? courtTypeId)
    {
        var selectedDate = date ?? VietnamClock.Today;
        var courts = await _courtService.GetAllCourtsAsync(courtTypeId);
        var bookings = await _bookingService.GetBookingsByDateAsync(selectedDate, courtId);

        var viewModel = new BookingPageViewModel
        {
            Courts = courts,
            Bookings = bookings,
            SelectedDate = selectedDate,
            SelectedCourtId = courtId,
            SelectedCourtTypeId = courtTypeId
        };

        return View(viewModel);
    }

    // Lịch đặt của chính người dùng đang đăng nhập
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> My()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var bookings = await _bookingService.GetBookingsByUserAsync(userId.Value);
        return View(bookings);
    }

    // Xử lý form đặt sân. UserId luôn lấy từ cookie đăng nhập, bỏ qua mọi giá trị UserId do form gửi lên.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        request.UserId = userId.Value;
        var result = await _bookingService.CreateBookingAsync(request);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Schedule", "Home", new { id = request.CourtId, date = request.BookingDate.ToString("yyyy-MM-dd") });
        }

        TempData["SuccessMessage"] = $"{result.Message} Tổng tiền: {result.Data?.TotalPrice:N0} VNĐ.";
        return RedirectToAction(nameof(My));
    }

    // API nhỏ trả về JSON để kiểm tra nhanh khung giờ trống bằng JavaScript/AJAX
    [HttpGet]
    public async Task<IActionResult> CheckAvailability(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        bool available = await _bookingService.IsTimeSlotAvailableAsync(courtId, date, startTime, endTime);
        return Json(new
        {
            available,
            message = available ? "Khung giờ còn trống!" : "Khung giờ này đã có người đặt!"
        });
    }
}
