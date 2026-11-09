using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Web.Models;

namespace SportsCourtBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(string? sport, string? area, DateOnly? date)
    {
        var courts = GetCourts()
            .Where(court => string.IsNullOrWhiteSpace(sport)
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

    public IActionResult Schedule(int id, DateOnly? date)
    {
        var court = GetCourts().SingleOrDefault(item => item.Id == id);
        if (court is null)
        {
            return NotFound();
        }

        return View(CreateSchedule(court, date ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1))));
    }

    public IActionResult Review(int id, DateOnly? date, int[]? selectedHours)
    {
        var court = GetCourts().SingleOrDefault(item => item.Id == id);
        if (court is null)
        {
            return NotFound();
        }

        var playDate = date ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var schedule = CreateSchedule(court, playDate);
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

    private static IReadOnlyList<CourtCardViewModel> GetCourts() =>
    [
        new()
        {
            Id = 1,
            FacilityName = "Green Park",
            CourtName = "Sân cầu lông số 2",
            Sport = "Cầu lông",
            District = "Quận 7",
            City = "TP. Hồ Chí Minh",
            OpeningHours = "06:00–23:00",
            StartingPrice = 100_000,
            Description = "Không gian thoáng, sàn đạt chuẩn và có khu vực nghỉ.",
            VisualClass = "court-visual--green"
        },
        new()
        {
            Id = 2,
            FacilityName = "Riverside Sports",
            CourtName = "Sân cầu lông số 1",
            Sport = "Cầu lông",
            District = "Bình Thạnh",
            City = "TP. Hồ Chí Minh",
            OpeningHours = "05:30–22:00",
            StartingPrice = 120_000,
            Description = "Sân trong nhà, ánh sáng tốt và dễ dàng di chuyển.",
            VisualClass = "court-visual--blue"
        },
        new()
        {
            Id = 3,
            FacilityName = "Central Football",
            CourtName = "Sân bóng đá mini A",
            Sport = "Bóng đá",
            District = "Thủ Đức",
            City = "TP. Hồ Chí Minh",
            OpeningHours = "06:00–23:00",
            StartingPrice = 350_000,
            Description = "Sân cỏ nhân tạo, phù hợp cho các trận đấu nhóm.",
            VisualClass = "court-visual--orange"
        }
    ];

    private static CourtScheduleViewModel CreateSchedule(CourtCardViewModel court, DateOnly playDate)
    {
        var slots = Enumerable.Range(6, 16)
            .Select(hour =>
            {
                var isUnavailable = court.Id switch
                {
                    1 => hour is 11 or 16,
                    2 => hour is 9 or 18,
                    _ => hour is 12 or 20
                };
                var price = court.StartingPrice + (hour >= 17 ? 50_000 : 0);

                return new CourtSlotViewModel
                {
                    StartHour = hour,
                    Price = price,
                    IsAvailable = !isUnavailable,
                    StatusLabel = isUnavailable ? "Đã có người đặt" : "Còn trống"
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
