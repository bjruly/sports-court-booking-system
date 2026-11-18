using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Extensions;
using SportsCourtBooking.Web.Models;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Controllers;

public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly ICourtService _courtService;
    private readonly IReviewService _reviewService;

    public BookingController(
        IBookingService bookingService,
        ICourtService courtService,
        IReviewService reviewService)
    {
        _bookingService = bookingService;
        _courtService = courtService;
        _reviewService = reviewService;
    }

    // Xem lịch đặt của tất cả khách theo ngày và sân (chỉ nhân viên / quản trị viên, vì có tên khách hàng)
    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, int? courtId, int? courtTypeId)
    {
        var selectedDate = date ?? VietnamClock.Today;
        var courts = await _courtService.GetAllCourtsAsync(courtTypeId);
        var bookings = await _bookingService.GetBookingsByDateAsync(selectedDate, courtId, includeInactive: true);

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

    // Admin/Staff xác nhận đơn đang chờ. Id người thực hiện lấy từ cookie đăng nhập để ghi log.
    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, DateOnly? date, int? courtId)
    {
        var actorId = User.GetUserId();
        if (actorId is null)
        {
            return Challenge();
        }

        var result = await _bookingService.ConfirmBookingAsync(id, actorId.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { date = date?.ToString("yyyy-MM-dd"), courtId });
    }

    // Admin/Staff hủy đơn Pending hoặc Confirmed; khung giờ được nhả cho khách khác.
    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, DateOnly? date, int? courtId)
    {
        var actorId = User.GetUserId();
        if (actorId is null)
        {
            return Challenge();
        }

        var result = await _bookingService.CancelBookingAsync(id, actorId.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { date = date?.ToString("yyyy-MM-dd"), courtId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Challenge();
        }

        var booking = await _bookingService.GetOwnBookingAsync(id, userId.Value);

        if (booking is null)
        {
            return NotFound();
        }

        return View(booking);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var form = await _reviewService.GetCustomerReviewFormAsync(id, userId.Value);
        if (form is null)
        {
            return NotFound();
        }

        return View(CreateReviewFormViewModel(form));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, CustomerReviewFormViewModel model)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var form = await _reviewService.GetCustomerReviewFormAsync(id, userId.Value);
        if (form is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(CreateReviewFormViewModel(form, model));
        }

        var result = await _reviewService.SaveCustomerReviewAsync(id, userId.Value, model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    private static CustomerReviewFormViewModel CreateReviewFormViewModel(
        CustomerReviewFormDto form,
        CustomerReviewFormViewModel? submitted = null)
        => new()
        {
            BookingId = form.BookingId,
            CourtName = form.CourtName,
            BookingDate = form.BookingDate,
            StartTime = form.StartTime,
            EndTime = form.EndTime,
            IsEditing = form.IsEditing,
            Rating = submitted?.Rating ?? form.Rating,
            Comment = submitted is null ? form.Comment : submitted.Comment
        };

    // Lịch đặt của chính người dùng đang đăng nhập
    [Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CancelMy(int id)
{
    var userId = User.GetUserId();

    if (userId is null)
    {
        return Challenge();
    }

    var booking = await _bookingService.GetOwnBookingAsync(
        id,
        userId.Value
    );

    if (booking is null)
    {
        return NotFound();
    }

    var result = await _bookingService.CancelOwnBookingAsync(
        id,
        userId.Value
    );

    if (result.Success)
    {
        TempData["SuccessMessage"] = result.Message;
    }
    else
    {
        TempData["ErrorMessage"] = result.Message;
    }

    return RedirectToAction(nameof(Details), new { id });
}
    [Authorize]
    [HttpGet]
public async Task<IActionResult> My(
    BookingStatus? status = null,
    DateOnly? fromDate = null,
    DateOnly? toDate = null)
{
    var userId = User.GetUserId();

    if (userId is null)
    {
        return Challenge();
    }

    if (!ModelState.IsValid)
    {
        TempData["ErrorMessage"] =
            "Dữ liệu bộ lọc không hợp lệ. Vui lòng chọn lại.";

        return RedirectToAction(nameof(My));
    }

    if (status.HasValue
        && !Enum.IsDefined(typeof(BookingStatus), status.Value))
    {
        TempData["ErrorMessage"] =
            "Trạng thái được chọn không hợp lệ.";

        return RedirectToAction(nameof(My));
    }

    if (fromDate.HasValue
        && toDate.HasValue
        && fromDate.Value > toDate.Value)
    {
        TempData["ErrorMessage"] =
            "Từ ngày phải trước hoặc bằng Đến ngày.";

        return RedirectToAction(nameof(My));
    }

    var bookings = await _bookingService.GetBookingsByUserAsync(
        userId.Value,
        status,
        fromDate,
        toDate
    );

    var viewModel = new BookingHistoryViewModel
    {
        Bookings = bookings,
        Status = status,
        FromDate = fromDate,
        ToDate = toDate
    };

    return View(viewModel);
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
