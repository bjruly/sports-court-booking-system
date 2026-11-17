using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Web.Extensions;
using SportsCourtBooking.Web.Models;

namespace SportsCourtBooking.Web.Controllers;

[Authorize(Roles = "CourtOwner")]
public class OwnerController : Controller
{
    private readonly IOwnerDashboardService _dashboardService;
    private readonly IOwnerCourtService _courtService;
    private readonly IOwnerFacilityService _facilityService;
    private readonly IBookingService _bookingService;
    private readonly IUserService _userService;
    private readonly IVoucherService _voucherService;
    private readonly IReviewService _reviewService;
    private readonly IOwnerRevenueReportService _revenueReportService;

    public OwnerController(
        IOwnerDashboardService dashboardService,
        IOwnerCourtService courtService,
        IOwnerFacilityService facilityService,
        IBookingService bookingService,
        IUserService userService,
        IVoucherService voucherService,
        IReviewService reviewService,
        IOwnerRevenueReportService revenueReportService)
    {
        _dashboardService = dashboardService;
        _courtService = courtService;
        _facilityService = facilityService;
        _bookingService = bookingService;
        _userService = userService;
        _voucherService = voucherService;
        _reviewService = reviewService;
        _revenueReportService = revenueReportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var dashboard = await _dashboardService.GetDashboardAsync(ownerId.Value);
        return View(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var profile = await _userService.GetOwnerProfileAsync(ownerId.Value);
        if (profile is null)
        {
            return NotFound();
        }

        return View(new OwnerProfileViewModel
        {
            FullName = profile.FullName,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            CreatedAt = profile.CreatedAt
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(OwnerProfileViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var currentProfile = await _userService.GetOwnerProfileAsync(ownerId.Value);
        if (currentProfile is null)
        {
            return NotFound();
        }

        model.Email = currentProfile.Email;
        model.CreatedAt = currentProfile.CreatedAt;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.UpdateOwnerProfileAsync(ownerId.Value, model);
        if (!result.Success || result.Profile is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await RefreshOwnerNameClaimAsync(result.Profile.FullName);
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (User.GetUserId() is null)
        {
            return Forbid();
        }

        return View(new OwnerChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(OwnerChangePasswordViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ClearPasswordFormInputs(model);
            return View(model);
        }

        var result = await _userService.ChangeOwnerPasswordAsync(ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ClearPasswordFormInputs(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    public async Task<IActionResult> Facilities()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        return View(await _facilityService.GetOwnerFacilitiesAsync(ownerId.Value));
    }

    [HttpGet]
    public IActionResult CreateFacility()
    {
        if (User.GetUserId() is null)
        {
            return Forbid();
        }

        return View(new OwnerFacilityFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFacility(OwnerFacilityFormViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _facilityService.CreateOwnerFacilityAsync(ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Facilities));
    }

    [HttpGet]
    public async Task<IActionResult> EditFacility(int id)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var facility = await _facilityService.GetOwnerFacilityAsync(id, ownerId.Value);
        if (facility is null)
        {
            return NotFound();
        }

        return View(new OwnerFacilityFormViewModel
        {
            Name = facility.Name,
            Address = facility.Address,
            Description = facility.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFacility(int id, OwnerFacilityFormViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _facilityService.UpdateOwnerFacilityAsync(id, ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Facilities));
    }

    [HttpGet]
    public async Task<IActionResult> Courts()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        return View(await _courtService.GetOwnerCourtsAsync(ownerId.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Pricing()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        return View(await _courtService.GetOwnerCourtsAsync(ownerId.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Vouchers()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        return View(await _voucherService.GetOwnerVouchersAsync(ownerId.Value));
    }

    [HttpGet]
    public IActionResult CreateVoucher()
        => View(new OwnerVoucherFormViewModel
        {
            StartDate = VietnamClock.Today,
            EndDate = VietnamClock.Today.AddDays(30)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVoucher(OwnerVoucherFormViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _voucherService.CreateOwnerVoucherAsync(ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Vouchers));
    }

    [HttpGet]
    public async Task<IActionResult> EditVoucher(int id)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var voucher = await _voucherService.GetOwnerVoucherAsync(id, ownerId.Value);
        if (voucher is null)
        {
            return NotFound();
        }

        return View(new OwnerVoucherFormViewModel
        {
            Id = voucher.Id,
            Code = voucher.Code,
            DiscountType = voucher.DiscountType,
            DiscountValue = voucher.DiscountValue,
            StartDate = voucher.StartDate,
            EndDate = voucher.EndDate,
            UsageLimit = voucher.UsageLimit,
            IsActive = voucher.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVoucher(int id, OwnerVoucherFormViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _voucherService.UpdateOwnerVoucherAsync(id, ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Vouchers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVoucherStatus(int id)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var result = await _voucherService.ToggleOwnerVoucherAsync(id, ownerId.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Vouchers));
    }

    [HttpGet]
    public async Task<IActionResult> Reviews(int? courtId)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var courts = await _courtService.GetOwnerCourtsAsync(ownerId.Value);
        if (courtId.HasValue && courts.All(court => court.Id != courtId.Value))
        {
            return NotFound();
        }

        return View(new OwnerReviewsViewModel
        {
            Courts = courts,
            Reviews = await _reviewService.GetOwnerReviewsAsync(ownerId.Value, courtId),
            SelectedCourtId = courtId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyToReview(int id, string reply, int? courtId)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var result = await _reviewService.ReplyToReviewAsync(id, ownerId.Value, reply);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Reviews), new { courtId });
    }

    [HttpGet]
    public async Task<IActionResult> RevenueReport(int? year, int? month, int? courtId)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var today = VietnamClock.Today;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;
        if (selectedYear < 1 || selectedYear > 9998 || selectedMonth < 1 || selectedMonth > 12)
        {
            return BadRequest();
        }

        var courts = await _courtService.GetOwnerCourtsAsync(ownerId.Value);
        if (courtId.HasValue && courts.All(court => court.Id != courtId.Value))
        {
            return NotFound();
        }

        return View(new OwnerRevenueReportViewModel
        {
            Courts = courts,
            Report = await _revenueReportService.GetMonthlyReportAsync(
                ownerId.Value, selectedYear, selectedMonth, courtId),
            SelectedYear = selectedYear,
            SelectedMonth = selectedMonth,
            SelectedCourtId = courtId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Bookings(DateOnly? date, int? courtId, BookingStatus? status)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid || (status.HasValue && !Enum.IsDefined(status.Value)))
        {
            return BadRequest();
        }

        return View(new OwnerBookingsViewModel
        {
            Courts = await _courtService.GetOwnerCourtsAsync(ownerId.Value),
            Bookings = await _bookingService.GetOwnerBookingsAsync(ownerId.Value, date, courtId, status),
            SelectedDate = date,
            SelectedCourtId = courtId,
            SelectedStatus = status
        });
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(DateOnly? date, int? courtId)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var courts = await _courtService.GetOwnerCourtsAsync(ownerId.Value);
        if (courtId.HasValue && courts.All(court => court.Id != courtId.Value))
        {
            return NotFound();
        }

        var selectedDate = date ?? VietnamClock.Today;
        var daysSinceMonday = ((int)selectedDate.DayOfWeek + 6) % 7;
        if (selectedDate.DayNumber < daysSinceMonday
            || selectedDate.DayNumber - daysSinceMonday > DateOnly.MaxValue.DayNumber - 6)
        {
            return BadRequest();
        }

        var weekStart = selectedDate.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(6);

        return View(new OwnerCalendarViewModel
        {
            Courts = courts,
            Bookings = await _bookingService.GetOwnerBookingsForCalendarAsync(
                ownerId.Value, weekStart, weekEnd, courtId),
            WeekStart = weekStart,
            SelectedCourtId = courtId
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateWalkInBooking(DateOnly? date, int? courtId)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var courts = await GetActiveOwnerCourtsAsync(ownerId.Value);
        if (courtId.HasValue && courts.All(court => court.Id != courtId.Value))
        {
            return NotFound();
        }

        var model = new OwnerWalkInBookingViewModel
        {
            Courts = courts,
            CourtId = courtId ?? courts.FirstOrDefault()?.Id ?? 0,
            BookingDate = date ?? VietnamClock.Today,
            StartHour = BookingRules.OpeningHour,
            EndHour = BookingRules.OpeningHour + 1
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWalkInBooking(OwnerWalkInBookingViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        model.Courts = await GetActiveOwnerCourtsAsync(ownerId.Value);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _bookingService.CreateOwnerWalkInBookingAsync(ownerId.Value, new OwnerWalkInBookingRequest
        {
            CourtId = model.CourtId,
            BookingDate = model.BookingDate,
            StartTime = new TimeOnly(model.StartHour, 0),
            EndTime = new TimeOnly(model.EndHour, 0),
            GuestName = model.GuestName,
            GuestPhone = model.GuestPhone,
            Note = model.Note
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Calendar), new { date = model.BookingDate.ToString("yyyy-MM-dd"), courtId = model.CourtId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBooking(int id, DateOnly? date, int? courtId, BookingStatus? status)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var result = await _bookingService.ConfirmOwnerBookingAsync(id, ownerId.Value, ownerId.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Bookings), new
        {
            date = date?.ToString("yyyy-MM-dd"),
            courtId,
            status = status?.ToString()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInBooking(
        int id,
        DateOnly? date,
        int? courtId,
        BookingStatus? status,
        bool returnToCalendar = false)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var result = await _bookingService.CheckInOwnerBookingAsync(id, ownerId.Value, ownerId.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        if (returnToCalendar)
        {
            return RedirectToAction(nameof(Calendar), new
            {
                date = date?.ToString("yyyy-MM-dd"),
                courtId
            });
        }

        return RedirectToAction(nameof(Bookings), new
        {
            date = date?.ToString("yyyy-MM-dd"),
            courtId,
            status = status?.ToString()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int id, DateOnly? date, int? courtId, BookingStatus? status)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var result = await _bookingService.CancelOwnerBookingAsync(id, ownerId.Value, ownerId.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Bookings), new
        {
            date = date?.ToString("yyyy-MM-dd"),
            courtId,
            status = status?.ToString()
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateCourt()
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var model = new OwnerCourtFormViewModel();
        await PopulateCourtFormOptionsAsync(model, ownerId.Value);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourt(OwnerCourtFormViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCourtFormOptionsAsync(model, ownerId.Value);
            return View(model);
        }

        var result = await _courtService.CreateOwnerCourtAsync(ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateCourtFormOptionsAsync(model, ownerId.Value);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Courts));
    }

    [HttpGet]
    public async Task<IActionResult> EditCourt(int id)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var court = await _courtService.GetOwnerCourtAsync(id, ownerId.Value);
        if (court is null)
        {
            return NotFound();
        }

        var model = new OwnerCourtFormViewModel
        {
            Name = court.Name,
            FacilityId = court.FacilityId,
            CourtTypeId = court.CourtTypeId,
            Location = court.Location,
            Description = court.Description,
            ImageUrl = court.ImageUrl,
            PricePerHour = court.PricePerHour,
            PeakStartHour = court.PeakStartHour,
            PeakSurcharge = court.PeakSurcharge,
            Status = court.Status
        };
        await PopulateCourtFormOptionsAsync(model, ownerId.Value);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourt(int id, OwnerCourtFormViewModel model)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCourtFormOptionsAsync(model, ownerId.Value);
            return View(model);
        }

        var result = await _courtService.UpdateOwnerCourtAsync(id, ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateCourtFormOptionsAsync(model, ownerId.Value);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Courts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCourtStatus(int id, CourtStatus status)
    {
        var ownerId = User.GetUserId();
        if (ownerId is null)
        {
            return Forbid();
        }

        var result = ModelState.IsValid
            ? await _courtService.UpdateOwnerCourtStatusAsync(id, ownerId.Value, status)
            : (false, "Trạng thái sân không hợp lệ.");

        if (result.Item1)
        {
            TempData["SuccessMessage"] = result.Item2;
        }
        else
        {
            TempData["ErrorMessage"] = result.Item2;
        }

        return RedirectToAction(nameof(Courts));
    }

    private async Task PopulateCourtFormOptionsAsync(OwnerCourtFormViewModel model, int ownerId)
    {
        model.CourtTypes = (await _courtService.GetCourtTypesAsync())
            .Select(courtType => new SelectListItem(courtType.Name, courtType.Id.ToString()))
            .ToList();
        model.Facilities = (await _facilityService.GetOwnerFacilitiesAsync(ownerId))
            .Select(facility => new SelectListItem(facility.Name, facility.Id.ToString()))
            .ToList();
    }

    private async Task RefreshOwnerNameClaimAsync(string fullName)
    {
        var claims = User.Claims
            .Where(claim => claim.Type != ClaimTypes.Name)
            .Append(new Claim(ClaimTypes.Name, fullName));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    private void ClearPasswordFormInputs(OwnerChangePasswordViewModel model)
    {
        var errorMessages = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToArray();

        model.CurrentPassword = string.Empty;
        model.NewPassword = string.Empty;
        model.ConfirmPassword = string.Empty;
        ModelState.Clear();
        foreach (var errorMessage in errorMessages)
        {
            ModelState.AddModelError(string.Empty, errorMessage);
        }
    }

    private async Task<List<CourtDto>> GetActiveOwnerCourtsAsync(int ownerId)
        => (await _courtService.GetOwnerCourtsAsync(ownerId))
            .Where(court => court.Status == CourtStatus.Active)
            .ToList();
}
