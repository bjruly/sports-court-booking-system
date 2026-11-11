using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
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

    // Xem lịch đặt sân theo ngày và sân
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, int? courtId, int? courtTypeId)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
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

    // Xử lý khi khách bấm nút "Đặt sân" (Form Submit)
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request)
    {
        // Nếu chưa có hệ thống Session, mặc định lấy UserId = 2 (Khách hàng mẫu trong SeedData)
        if (request.UserId <= 0)
        {
            request.UserId = 2;
        }

        var result = await _bookingService.CreateBookingAsync(request);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Schedule", "Home", new { date = request.BookingDate.ToString("yyyy-MM-dd"), courtId = request.CourtId });
        }

        TempData["SuccessMessage"] = $"{result.Message} Tổng tiền: {result.Data?.TotalPrice:N0} VNĐ.";
        return RedirectToAction("Schedule", "Home", new { date = request.BookingDate.ToString("yyyy-MM-dd"), courtId = request.CourtId });
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