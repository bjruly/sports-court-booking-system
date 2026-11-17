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
    private readonly IBookingService _bookingService;

    public OwnerController(
        IOwnerDashboardService dashboardService,
        IOwnerCourtService courtService,
        IBookingService bookingService)
    {
        _dashboardService = dashboardService;
        _courtService = courtService;
        _bookingService = bookingService;
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
        var model = new OwnerCourtFormViewModel();
        await PopulateCourtTypesAsync(model);
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
            await PopulateCourtTypesAsync(model);
            return View(model);
        }

        var result = await _courtService.CreateOwnerCourtAsync(ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateCourtTypesAsync(model);
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
            CourtTypeId = court.CourtTypeId,
            Location = court.Location,
            Description = court.Description,
            ImageUrl = court.ImageUrl,
            PricePerHour = court.PricePerHour,
            Status = court.Status
        };
        await PopulateCourtTypesAsync(model);
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
            await PopulateCourtTypesAsync(model);
            return View(model);
        }

        var result = await _courtService.UpdateOwnerCourtAsync(id, ownerId.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateCourtTypesAsync(model);
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

    private async Task PopulateCourtTypesAsync(OwnerCourtFormViewModel model)
    {
        model.CourtTypes = (await _courtService.GetCourtTypesAsync())
            .Select(courtType => new SelectListItem(courtType.Name, courtType.Id.ToString()))
            .ToList();
    }

    private async Task<List<CourtDto>> GetActiveOwnerCourtsAsync(int ownerId)
        => (await _courtService.GetOwnerCourtsAsync(ownerId))
            .Where(court => court.Status == CourtStatus.Active)
            .ToList();
}
