using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Models;

namespace SportsCourtBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICourtService _courtService;
    private readonly IBookingService _bookingService;

    public HomeController(
        ILogger<HomeController> logger,
        ICourtService courtService,
        IBookingService bookingService)
    {
        _logger = logger;
        _courtService = courtService;
        _bookingService = bookingService;
    }

    public async Task<IActionResult> Index(string? sport, string? area, DateOnly? date)
    {
        var allCourts = await LoadCourtCardsFromDbAsync();

        var courts = allCourts
            .Where(court => string.IsNullOrWhiteSpace(sport)
                || court.Sport.Contains(sport, StringComparison.OrdinalIgnoreCase)
                || string.Equals(court.Sport, sport, StringComparison.OrdinalIgnoreCase))
            .Where(court => string.IsNullOrWhiteSpace(area)
                || court.District.Contains(area, StringComparison.OrdinalIgnoreCase)
                || court.City.Contains(area, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return View(new CourtCatalogViewModel
        {
            Sport = sport,
            Area = area,
            PlayDate = date ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Courts = courts
        });
    }

    public async Task<IActionResult> Schedule(int id, DateOnly? date)
    {
        var allCourts = await LoadCourtCardsFromDbAsync();
        var court = allCourts.SingleOrDefault(item => item.Id == id);
        if (court is null)
        {
            return NotFound();
        }

        var playDate = date ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var schedule = await CreateScheduleFromDbAsync(court, playDate);

        if (TempData["ErrorMessage"] is string errorMsg)
        {
            schedule = new CourtScheduleViewModel
            {
                Court = schedule.Court,
                PlayDate = schedule.PlayDate,
                Slots = schedule.Slots,
                ErrorMessage = errorMsg
            };
        }

        return View(schedule);
    }

    public async Task<IActionResult> Review(int id, DateOnly? date, int[]? selectedHours)
    {
        var allCourts = await LoadCourtCardsFromDbAsync();
        var court = allCourts.SingleOrDefault(item => item.Id == id);
        if (court is null)
        {
            return NotFound();
        }

        var playDate = date ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var schedule = await CreateScheduleFromDbAsync(court, playDate);
        var hours = selectedHours?.Distinct().Order().ToArray() ?? [];
        var selectedSlots = schedule.Slots.Where(slot => hours.Contains(slot.StartHour)).ToArray();

        var isContiguous = hours.Length > 0
            && hours.Length <= 4
            && hours.Zip(hours.Skip(1), (current, next) => next == current + 1).All(isNext => isNext);
        var areAvailable = selectedSlots.Length == hours.Length && selectedSlots.All(slot => slot.IsAvailable);

        if (!isContiguous || !areAvailable)
        {
            schedule = new CourtScheduleViewModel
            {
                Court = court,
                PlayDate = playDate,
                Slots = schedule.Slots,
                ErrorMessage = "Vui lòng chọn từ 1 đến 4 khung giờ liên tiếp còn trống."
            };

            return View("Schedule", schedule);
        }

        var returnUrl = Url.Action(nameof(Review), new
        {
            id,
            date = playDate.ToString("yyyy-MM-dd"),
            selectedHours = hours
        }) ?? Url.Action(nameof(Index))!;

        return View(new BookingReviewViewModel
        {
            Court = court,
            PlayDate = playDate,
            Slots = selectedSlots,
            ReturnUrl = returnUrl
        });
    }

    // Xử lý lưu đơn đặt sân thật vào SQL Server từ trang Review
    [HttpPost]
    public async Task<IActionResult> ConfirmBooking(int id, DateOnly date, int[] selectedHours, string? note)
    {
        var hours = selectedHours?.Distinct().Order().ToArray() ?? [];
        if (hours.Length == 0)
        {
            TempData["ErrorMessage"] = "Chưa chọn khung giờ đặt sân.";
            return RedirectToAction(nameof(Schedule), new { id, date = date.ToString("yyyy-MM-dd") });
        }

        // Lấy UserId từ Cookie nếu đã đăng nhập, nếu chưa thì dùng UserId = 2 (Khách hàng mẫu)
        int userId = 2;
        if (Request.Cookies.TryGetValue("CurrentUserId", out var cookieUserId) && int.TryParse(cookieUserId, out var parsedId))
        {
            userId = parsedId;
        }

        var startTime = new TimeOnly(hours.First(), 0);
        var endTime = new TimeOnly(hours.Last() + 1, 0);

        var result = await _bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = userId,
            CourtId = id,
            BookingDate = date,
            StartTime = startTime,
            EndTime = endTime,
            Note = note
        });

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Schedule), new { id, date = date.ToString("yyyy-MM-dd") });
        }

        TempData["SuccessMessage"] = $"Đặt sân thành công khung giờ {startTime:HH:mm} - {endTime:HH:mm} ngày {date:dd/MM/yyyy}!";
        return RedirectToAction(nameof(Schedule), new { id, date = date.ToString("yyyy-MM-dd") });
    }

    public IActionResult Login(string? returnUrl)
    {
        return View(new AccountPrototypeViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index))
        });
    }

    public IActionResult Register(string? returnUrl)
    {
        return View(new AccountPrototypeViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index))
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Chuyển đổi dữ liệu Sân từ SQL Server sang CourtCardViewModel của giao diện
    private async Task<IReadOnlyList<CourtCardViewModel>> LoadCourtCardsFromDbAsync()
    {
        var dbCourts = await _courtService.GetAllCourtsAsync();

        var visualClasses = new[] { "court-visual--green", "court-visual--blue", "court-visual--orange" };

        return dbCourts.Select((c, index) => new CourtCardViewModel
        {
            Id = c.Id,
            FacilityName = string.IsNullOrWhiteSpace(c.Location) ? "Sports Center" : c.Location,
            CourtName = c.Name,
            Sport = string.IsNullOrWhiteSpace(c.CourtTypeName) ? "Cầu lông" : c.CourtTypeName,
            District = c.Id switch
            {
                1 => "Quận 7",
                2 => "Bình Thạnh",
                3 => "Thủ Đức",
                _ => "Quận 10"
            },
            City = "TP. Hồ Chí Minh",
            OpeningHours = "06:00–22:00",
            StartingPrice = (int)c.PricePerHour,
            Description = c.Description ?? "Không gian thoáng, sân đạt chuẩn thi đấu.",
            VisualClass = visualClasses[index % visualClasses.Length]
        }).ToArray();
    }

    // Kiểm tra lịch đặt sân thật từ bảng Bookings trong SQL Server cho từng khung giờ (6h -> 21h)
    private async Task<CourtScheduleViewModel> CreateScheduleFromDbAsync(CourtCardViewModel court, DateOnly playDate)
    {
        var existingBookings = await _bookingService.GetBookingsByDateAsync(playDate, court.Id);

        var slots = Enumerable.Range(6, 16)
            .Select(hour =>
            {
                var slotStart = new TimeOnly(hour, 0);
                var slotEnd = new TimeOnly(hour + 1, 0);

                // Kiểm tra xem giờ này đã nằm trong đơn đặt sân nào trong Database chưa
                bool isBookedInDb = existingBookings.Any(b => slotStart < b.EndTime && slotEnd > b.StartTime);

                var price = court.StartingPrice + (hour >= 17 ? 50_000 : 0);

                return new CourtSlotViewModel
                {
                    StartHour = hour,
                    Price = price,
                    IsAvailable = !isBookedInDb,
                    StatusLabel = isBookedInDb ? "Đã có người đặt" : "Còn trống"
                };
            })
            .ToArray();

        return new CourtScheduleViewModel
        {
            Court = court,
            PlayDate = playDate,
            Slots = slots
        };
    }
}