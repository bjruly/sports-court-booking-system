using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Web.Extensions;
using SportsCourtBooking.Web.Models;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICourtService _courtService;
    private readonly IBookingService _bookingService;
    private readonly IVoucherService _voucherService;

    public HomeController(
        ILogger<HomeController> logger,
        ICourtService courtService,
        IBookingService bookingService,
        IVoucherService voucherService)
    {
        _logger = logger;
        _courtService = courtService;
        _bookingService = bookingService;
        _voucherService = voucherService;
    }

    public async Task<IActionResult> Index(string? sport, string? area, DateOnly? date, decimal? minPrice, decimal? maxPrice, string? sortBy, int? startHour, int? endHour)
    {
    if (!ModelState.IsValid)
        {
        TempData["ErrorMessage"] =
            "Dữ liệu tìm kiếm không hợp lệ. Vui lòng chọn lại.";

        return RedirectToAction(nameof(Index));
        }

    if ((minPrice.HasValue && minPrice.Value < 0)
        || (maxPrice.HasValue && maxPrice.Value < 0))
        {
        TempData["ErrorMessage"] =
            "Giá tìm kiếm không được là số âm.";

        return RedirectToAction(nameof(Index));
       }

    if (minPrice.HasValue
        && maxPrice.HasValue
        && minPrice.Value > maxPrice.Value)
       {
        TempData["ErrorMessage"] =
            "Giá thấp nhất phải nhỏ hơn hoặc bằng giá cao nhất.";

        return RedirectToAction(nameof(Index));
       }

    var playDate = date ?? VietnamClock.Today.AddDays(1);

    TimeOnly? searchStartTime = null;
    TimeOnly? searchEndTime = null;

    if (startHour.HasValue != endHour.HasValue)
    {
    TempData["ErrorMessage"] =
        "Vui lòng chọn đủ giờ bắt đầu và giờ kết thúc.";

    return RedirectToAction(nameof(Index));
    }

    if (startHour.HasValue && endHour.HasValue)
    {
    if (startHour.Value < BookingRules.OpeningHour
        || startHour.Value >= BookingRules.ClosingHour
        || endHour.Value <= BookingRules.OpeningHour
        || endHour.Value > BookingRules.ClosingHour)
    {
        TempData["ErrorMessage"] =
            "Vui lòng chọn giờ chơi trong khoảng 06:00–22:00.";

        return RedirectToAction(nameof(Index));
    }

    searchStartTime = new TimeOnly(startHour.Value, 0);
    searchEndTime = new TimeOnly(endHour.Value, 0);

    var timeError = BookingRules.ValidateTimeRange(
        playDate,
        searchStartTime.Value,
        searchEndTime.Value,
        VietnamClock.Now
    );

    if (timeError != null)
    {
        TempData["ErrorMessage"] = timeError;

        return RedirectToAction(nameof(Index));
    }
    }

    var allCourts = await LoadCourtCardsFromDbAsync(homeCatalogOnly: true);

    var courts = allCourts
        .Where(court =>
            string.IsNullOrWhiteSpace(sport)
            || court.Sport.Contains(
                sport,
                StringComparison.OrdinalIgnoreCase
            )
        )
        .Where(court =>
            string.IsNullOrWhiteSpace(area)
            || court.District.Contains(
                area,
                StringComparison.OrdinalIgnoreCase
            )
            || court.City.Contains(
                area,
                StringComparison.OrdinalIgnoreCase
            )
        );

    if (minPrice.HasValue)
       {
        courts = courts.Where(court =>
            court.StartingPrice >= minPrice.Value
        );
       }

    if (maxPrice.HasValue)
    {
        courts = courts.Where(court =>
            court.StartingPrice <= maxPrice.Value
        );
    }

    if (searchStartTime.HasValue && searchEndTime.HasValue)
    {
    var bookings = await _bookingService.GetBookingsByDateAsync(
        playDate
    );

    var busyCourtIds = bookings
        .Where(booking =>
            searchStartTime.Value < booking.EndTime
            && searchEndTime.Value > booking.StartTime
        )
        .Select(booking => booking.CourtId)
        .ToHashSet();

    courts = courts.Where(court =>
        !busyCourtIds.Contains(court.Id)
    );
    }

    var selectedSort = "default";

    if (sortBy == "price_asc")
    {
        courts = courts.OrderBy(court => court.StartingPrice);
        selectedSort = "price_asc";
    }
    else if (sortBy == "price_desc")
    {
        courts = courts.OrderByDescending(court => court.StartingPrice);
        selectedSort = "price_desc";
    }

    var viewModel = new CourtCatalogViewModel
    {
        Sport = sport,
        Area = area,
        PlayDate = date ?? VietnamClock.Today.AddDays(1),
        MinPrice = minPrice,
        MaxPrice = maxPrice,
        SortBy = selectedSort,
        Courts = courts.ToArray()
    };

    return View(viewModel);
    }

    public async Task<IActionResult> Schedule(int id, DateOnly? date)
    {
        var allCourts = await LoadCourtCardsFromDbAsync(homeCatalogOnly: false);
        var court = allCourts.SingleOrDefault(item => item.Id == id);
        if (court is null)
        {
            TempData["ErrorMessage"] = "Sân này đang bảo trì hoặc ngừng hoạt động. Vui lòng chọn sân khác.";
            return RedirectToAction(nameof(Index));
        }

        var siblingCourts = allCourts
            .Where(item => item.Sport == court.Sport && item.District == court.District)
            .OrderBy(item => item.Id)
            .ToArray();

        var playDate = date ?? VietnamClock.Today.AddDays(1);
        var schedule = await CreateScheduleFromDbAsync(court, playDate, siblingCourts);

        // Thông báo thành công/lỗi sau khi đặt sân (TempData) được _Layout hiển thị chung cho mọi trang.
        return View(schedule);
    }

    [HttpGet]
    public async Task<IActionResult> CourtDetails(
        int id,
        DateOnly? date)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Thông tin sân hoặc ngày chơi không hợp lệ.");
        }

        var court = await _courtService.GetCourtByIdAsync(id);

        if (court is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(court.Location) || court.Location.StartsWith("Khu", StringComparison.OrdinalIgnoreCase))
        {
            var district = string.IsNullOrWhiteSpace(court.District) ? "TP. Hồ Chí Minh" : court.District;
            court.Location = $"{district}, TP. Hồ Chí Minh";
        }

        // Lấy danh sách sân đang hoạt động của cơ sở (chỉ lấy sân Active, ẩn hoàn toàn các sân bảo trì hoặc ngừng hoạt động)
        var allActiveCourts = await _courtService.GetAllCourtsAsync(onlyActive: true);
        List<CourtDto> siblingCourts;
        if (court.FacilityId.HasValue)
        {
            siblingCourts = allActiveCourts
                .Where(c => c.FacilityId == court.FacilityId.Value)
                .OrderBy(c => c.Id)
                .ToList();
        }
        else
        {
            siblingCourts = allActiveCourts
                .Where(c => c.CourtTypeId == court.CourtTypeId && c.District == court.District)
                .OrderBy(c => c.Id)
                .ToList();
        }

        // Nếu sân hiện tại đang bảo trì hoặc ngừng hoạt động:
        if (court.Status != CourtStatus.Active)
        {
            // Nếu cơ sở còn sân khác đang hoạt động, chuyển sang sân hoạt động đầu tiên
            var activeSibling = siblingCourts.FirstOrDefault();
            if (activeSibling != null)
            {
                TempData["InfoMessage"] = $"Sân '{court.Name}' hiện đang bảo trì hoặc tạm ngưng hoạt động. Hệ thống đã tự động chuyển sang sân đang hoạt động cùng cơ sở.";
                return RedirectToAction(nameof(CourtDetails), new { id = activeSibling.Id, date = date?.ToString("yyyy-MM-dd") });
            }

            // Nếu toàn bộ sân của cơ sở đều bảo trì / tạm ngưng, quay về trang chủ
            TempData["ErrorMessage"] = "Toàn bộ sân tại cơ sở này hiện đang tạm ngưng hoặc bảo trì. Quý khách vui lòng chọn cơ sở khác.";
            return RedirectToAction(nameof(Index));
        }

        if (siblingCourts.Count == 0)
        {
            siblingCourts = new List<CourtDto> { court };
        }

        var viewModel = new CourtDetailsViewModel
        {
            Court = court,
            PlayDate = date ?? VietnamClock.Today.AddDays(1),
            AvailableCourts = siblingCourts
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Review(
        int id,
        DateOnly? date,
        int[]? selectedHours,
        string? voucherCode)
    {
        var allCourts = await LoadCourtCardsFromDbAsync(homeCatalogOnly: false);
        var court = allCourts.SingleOrDefault(item => item.Id == id);
        if (court is null)
        {
            TempData["ErrorMessage"] = "Sân này đang bảo trì hoặc ngừng hoạt động. Vui lòng chọn sân khác.";
            return RedirectToAction(nameof(Index));
        }

        var playDate = date ?? VietnamClock.Today.AddDays(1);
        var schedule = await CreateScheduleFromDbAsync(court, playDate);

        var hasValidHours = BookingRules.TryNormalizeHours(selectedHours, out var hours, out var hoursError);
        var selectedSlots = hasValidHours
            ? schedule.Slots.Where(slot => hours.Contains(slot.StartHour)).ToArray()
            : Array.Empty<CourtSlotViewModel>();
        var areAvailable = hasValidHours
            && selectedSlots.Length == hours.Length
            && selectedSlots.All(slot => slot.IsAvailable);

        if (!areAvailable)
        {
            schedule = new CourtScheduleViewModel
            {
                Court = court,
                PlayDate = playDate,
                Slots = schedule.Slots,
                ErrorMessage = hoursError
                    ?? "Khung giờ bạn chọn đã có người đặt hoặc đã qua. Vui lòng chọn lại."
            };

            return View("Schedule", schedule);
        }

        var returnUrl = Url.Action(nameof(Review), new
        {
            id,
            date = playDate.ToString("yyyy-MM-dd"),
            selectedHours = hours,
            voucherCode
        }) ?? Url.Action(nameof(Index))!;

        var voucher = string.IsNullOrWhiteSpace(voucherCode)
            ? null
            : await _voucherService.ValidateForCourtAsync(
                id, VietnamClock.Today, selectedSlots.Sum(slot => slot.Price), voucherCode);

        return View(new BookingReviewViewModel
        {
            Court = court,
            PlayDate = playDate,
            Slots = selectedSlots,
            ReturnUrl = returnUrl,
            VoucherCode = voucher?.Code,
            VoucherError = voucher is { Success: false } ? voucher.Message : null,
            DiscountAmount = voucher is { Success: true } ? voucher.DiscountAmount : 0m
        });
    }

    // Nếu phiên đăng nhập hết hạn lúc đang ở trang Review, sau khi đăng nhập lại trình duyệt sẽ quay về
    // địa chỉ này bằng GET. Chuyển về trang chủ kèm hướng dẫn thay vì báo lỗi 405.
    [HttpGet]
    public IActionResult ConfirmBooking()
    {
        TempData["ErrorMessage"] = "Vui lòng chọn lại sân và khung giờ để tiếp tục đặt sân.";
        return RedirectToAction(nameof(Index));
    }

    // Lưu đơn đặt sân thật vào PostgreSQL. Bắt buộc đăng nhập; người đặt luôn là người trong cookie đăng nhập.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBooking(
        int id,
        DateOnly date,
        int[]? selectedHours,
        string? note,
        string? voucherCode)
    {
        // Kiểm tra phía server: gửi thẳng request (bỏ qua giao diện) cũng không thể đặt giờ sai quy tắc.
        if (!BookingRules.TryNormalizeHours(selectedHours, out var hours, out var hoursError))
        {
            TempData["ErrorMessage"] = hoursError;
            return RedirectToAction(nameof(Schedule), new { id, date = date.ToString("yyyy-MM-dd") });
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        // TryNormalizeHours đã đảm bảo giờ nằm trong 06..21 nên hours[^1] + 1 tối đa là 22.
        var startTime = new TimeOnly(hours[0], 0);
        var endTime = new TimeOnly(hours[^1] + 1, 0);

        var result = await _bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = userId.Value,
            CourtId = id,
            BookingDate = date,
            StartTime = startTime,
            EndTime = endTime,
            Note = note,
            VoucherCode = voucherCode
        });

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Review), new
            {
                id,
                date = date.ToString("yyyy-MM-dd"),
                selectedHours = hours,
                voucherCode
            });
        }

        TempData["SuccessMessage"] =
            $"Đã tạo đơn đặt sân {startTime:HH:mm} - {endTime:HH:mm} ngày {date:dd/MM/yyyy}, tổng {result.Data?.TotalPrice:N0}₫. Đơn đang chờ xác nhận.";
        return RedirectToAction("My", "Booking");
    }

    public IActionResult Login(string? returnUrl)
    {
        return View(new AccountPrototypeViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        });
    }

    public IActionResult Register(string? returnUrl)
    {
        return View(new AccountPrototypeViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index))
        });
    }

    public IActionResult AccessDenied()
    {
        return View();
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

    // Chuyển đổi dữ liệu Sân từ PostgreSQL sang CourtCardViewModel của giao diện (chỉ lấy sân và cơ sở đã được duyệt và đang hoạt động)
    private async Task<IReadOnlyList<CourtCardViewModel>> LoadCourtCardsFromDbAsync(bool homeCatalogOnly = false)
    {
        var dbCourts = await _courtService.GetAllCourtsAsync(onlyActive: true);
        var visualClasses = new[] { "court-visual--green", "court-visual--blue", "court-visual--orange" };

        if (homeCatalogOnly)
        {
            // Mỗi Cơ sở thể thao đại diện cho 1 tấm thẻ trên trang chủ
            var facilityGroups = dbCourts
                .GroupBy(c => c.FacilityId.HasValue ? $"fac_{c.FacilityId.Value}" : $"type_{c.CourtTypeId}_{c.District}")
                .ToList();

            var cards = new List<CourtCardViewModel>();
            int cardIndex = 0;

            foreach (var group in facilityGroups)
            {
                var groupList = group.OrderBy(c => c.Id).ToList();
                var primaryCourt = groupList.First();

                var district = string.IsNullOrWhiteSpace(primaryCourt.District) ? "TP. Hồ Chí Minh" : primaryCourt.District;
                var facilityName = !string.IsNullOrWhiteSpace(primaryCourt.FacilityName)
                    ? primaryCourt.FacilityName
                    : primaryCourt.Location;

                var courtNamesSummary = string.Join(", ", groupList.Select(c =>
                {
                    var name = c.Name;
                    var parenIdx = name.IndexOf('(');
                    return parenIdx > 0 ? name[..parenIdx].Trim() : name;
                }));

                cards.Add(new CourtCardViewModel
                {
                    Id = primaryCourt.Id,
                    FacilityName = facilityName,
                    CourtName = primaryCourt.Name,
                    Sport = string.IsNullOrWhiteSpace(primaryCourt.CourtTypeName) ? "Cầu lông" : primaryCourt.CourtTypeName,
                    District = district,
                    City = "TP. Hồ Chí Minh",
                    OpeningHours = $"{BookingRules.OpeningHour:00}:00–{BookingRules.ClosingHour:00}:00",
                    StartingPrice = groupList.Min(c => c.PricePerHour),
                    PeakStartHour = primaryCourt.PeakStartHour,
                    PeakSurcharge = primaryCourt.PeakSurcharge,
                    Description = primaryCourt.Description ?? "Không gian thoáng, sân đạt chuẩn thi đấu.",
                    VisualClass = visualClasses[cardIndex % visualClasses.Length],
                    CourtCount = groupList.Count,
                    CourtNamesSummary = courtNamesSummary
                });

                cardIndex++;
            }

            return cards;
        }

        return dbCourts.Select((c, index) =>
        {
            var district = string.IsNullOrWhiteSpace(c.District) ? "TP. Hồ Chí Minh" : c.District;
            var facilityName = !string.IsNullOrWhiteSpace(c.FacilityName) ? c.FacilityName : c.Location;

            return new CourtCardViewModel
            {
                Id = c.Id,
                FacilityName = facilityName,
                CourtName = c.Name,
                Sport = string.IsNullOrWhiteSpace(c.CourtTypeName) ? "Cầu lông" : c.CourtTypeName,
                District = district,
                City = "TP. Hồ Chí Minh",
                OpeningHours = $"{BookingRules.OpeningHour:00}:00–{BookingRules.ClosingHour:00}:00",
                StartingPrice = c.PricePerHour,
                PeakStartHour = c.PeakStartHour,
                PeakSurcharge = c.PeakSurcharge,
                Description = c.Description ?? "Không gian thoáng, sân đạt chuẩn thi đấu.",
                VisualClass = visualClasses[index % visualClasses.Length],
                CourtCount = 1,
                CourtNamesSummary = c.Name
            };
        }).ToArray();
    }

    // Kiểm tra lịch đặt sân thật từ bảng Bookings cho từng khung giờ (06:00 -> 22:00).
    // Khung giờ đã trôi qua (theo giờ Việt Nam) cũng không cho chọn.
    private async Task<CourtScheduleViewModel> CreateScheduleFromDbAsync(
        CourtCardViewModel court,
        DateOnly playDate,
        IReadOnlyList<CourtCardViewModel>? siblingCourts = null)
    {
        var existingBookings = await _bookingService.GetBookingsByDateAsync(playDate, court.Id);
        var now = VietnamClock.Now;

        var slots = Enumerable.Range(BookingRules.OpeningHour, BookingRules.ClosingHour - BookingRules.OpeningHour)
            .Select(hour =>
            {
                var slotStart = new TimeOnly(hour, 0);
                var slotEnd = new TimeOnly(hour + 1, 0);

                // Kiểm tra xem giờ này đã nằm trong đơn đặt sân nào trong Database chưa
                var isBookedInDb = existingBookings.Any(b => slotStart < b.EndTime && slotEnd > b.StartTime);
                var isPast = BookingRules.IsSlotInPast(playDate, hour, now);

                return new CourtSlotViewModel
                {
                    StartHour = hour,
                    // Cùng công thức giá với BookingService nên giá hiển thị luôn khớp giá lưu trong database
                    Price = BookingRules.GetSlotPrice(
                        court.StartingPrice, hour, court.PeakStartHour, court.PeakSurcharge),
                    IsAvailable = !isBookedInDb && !isPast,
                    StatusLabel = isBookedInDb ? "Đã có người đặt" : isPast ? "Đã qua" : "Còn trống"
                };
            })
            .ToArray();

        return new CourtScheduleViewModel
        {
            Court = court,
            PlayDate = playDate,
            Slots = slots,
            SiblingCourts = siblingCourts ?? []
        };
    }
}
