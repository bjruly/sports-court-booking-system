using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Web.Extensions;

namespace SportsCourtBooking.Web.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // Thông báo của Admin hiển thị trong ứng dụng WinForms; trên web chỉ có Chủ sân và khách hàng.
    [HttpGet("/api/notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var role = User.IsInRole(nameof(UserRole.CourtOwner)) ? UserRole.CourtOwner : UserRole.Customer;
        return Json(new NotificationResponseDto
        {
            Success = true,
            Items = await _notificationService.GetNotificationsAsync(userId.Value, role)
        });
    }
}
