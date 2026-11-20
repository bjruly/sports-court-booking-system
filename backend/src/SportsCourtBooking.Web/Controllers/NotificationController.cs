using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Web.Extensions;

namespace SportsCourtBooking.Web.Controllers;

[Authorize(Roles = "Admin,CourtOwner")]
public class NotificationController : Controller
{
    private readonly AppDbContext _context;

    public NotificationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("/api/notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var isOwner = User.IsInRole("CourtOwner");
        var isAdmin = User.IsInRole("Admin");

        var notifications = new List<NotificationItemDto>();

        if (isAdmin)
        {
            // 1. Cơ sở mới tạo hoặc sửa chờ Admin duyệt
            var pendingFacilitiesRaw = await _context.Facilities
                .AsNoTracking()
                .Include(f => f.Owner)
                .Where(f => !f.IsApproved)
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToListAsync();

            var pendingFacilities = pendingFacilitiesRaw.Select(f => new NotificationItemDto
            {
                Id = $"fac_{f.Id}_{f.UpdatedAt?.Ticks ?? f.CreatedAt.Ticks}",
                Title = "Cơ sở mới chờ duyệt",
                Message = $"Cơ sở '{f.Name}' của chủ sân {(f.Owner != null ? f.Owner.FullName : "đang đăng ký")} đang chờ duyệt.",
                Type = "facility",
                Url = "/Admin/Facilities",
                CreatedAt = f.UpdatedAt ?? f.CreatedAt
            });
            notifications.AddRange(pendingFacilities);

            // 2. Đơn đặt sân mới nhất trong toàn hệ thống
            var recentBookingsRaw = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Court)
                .OrderByDescending(b => b.CreatedAt)
                .Take(6)
                .ToListAsync();

            var recentBookings = recentBookingsRaw.Select(b => new NotificationItemDto
            {
                Id = $"booking_{b.Id}",
                Title = $"Đơn đặt sân mới #{b.Id}",
                Message = $"Khách {(b.User != null ? b.User.FullName : b.GuestName ?? "Khách")} vừa đặt sân {(b.Court != null ? b.Court.Name : "sân")} ({b.BookingDate:dd/MM/yyyy}).",
                Type = "booking",
                Url = "/Admin/Bookings",
                CreatedAt = b.CreatedAt
            });
            notifications.AddRange(recentBookings);

            // 3. Đánh giá mới nhất từ người dùng
            var recentReviewsRaw = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Court)
                .OrderByDescending(r => r.CreatedAt)
                .Take(4)
                .ToListAsync();

            var recentReviews = recentReviewsRaw.Select(r => new NotificationItemDto
            {
                Id = $"review_{r.Id}",
                Title = $"Đánh giá mới {r.Rating}★",
                Message = $"{(r.User != null ? r.User.FullName : "Khách")} đánh giá {(r.Court != null ? r.Court.Name : "sân")}: \"{(r.Comment != null && r.Comment.Length > 45 ? r.Comment.Substring(0, 45) + "..." : r.Comment ?? "")}\"",
                Type = "review",
                Url = "/Admin/Reviews",
                CreatedAt = r.CreatedAt
            });
            notifications.AddRange(recentReviews);
        }
        else if (isOwner)
        {
            // 1. Đơn đặt sân mới trên các sân thuộc quyền quản lý của Owner
            var ownerBookingsRaw = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Court)
                .Where(b => b.Court != null && b.Court.OwnerId == userId.Value)
                .OrderByDescending(b => b.CreatedAt)
                .Take(8)
                .ToListAsync();

            var ownerBookings = ownerBookingsRaw.Select(b => new NotificationItemDto
            {
                Id = $"booking_{b.Id}",
                Title = $"Khách đặt sân #{b.Id}",
                Message = $"Khách {(b.User != null ? b.User.FullName : b.GuestName ?? "Khách")} đặt {(b.Court != null ? b.Court.Name : "sân")} ({b.StartTime:hh\\:mm}-{b.EndTime:hh\\:mm}, ngày {b.BookingDate:dd/MM}).",
                Type = "booking",
                Url = "/Owner/Bookings",
                CreatedAt = b.CreatedAt
            });
            notifications.AddRange(ownerBookings);

            // 2. Đánh giá mới trên các sân của Owner
            var ownerReviewsRaw = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Court)
                .Where(r => r.Court != null && r.Court.OwnerId == userId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            var ownerReviews = ownerReviewsRaw.Select(r => new NotificationItemDto
            {
                Id = $"review_{r.Id}",
                Title = $"Đánh giá mới {r.Rating}★",
                Message = $"Khách {(r.User != null ? r.User.FullName : "Khách")} nhận xét {(r.Court != null ? r.Court.Name : "sân")}: \"{(r.Comment != null && r.Comment.Length > 45 ? r.Comment.Substring(0, 45) + "..." : r.Comment ?? "")}\"",
                Type = "review",
                Url = "/Owner/Reviews",
                CreatedAt = r.CreatedAt
            });
            notifications.AddRange(ownerReviews);

            // 3. Cơ sở của Owner đã được Admin phê duyệt
            var approvedFacilitiesRaw = await _context.Facilities
                .AsNoTracking()
                .Where(f => f.OwnerId == userId.Value && f.IsApproved)
                .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                .Take(3)
                .ToListAsync();

            var approvedFacilities = approvedFacilitiesRaw.Select(f => new NotificationItemDto
            {
                Id = $"fac_approved_{f.Id}_{f.UpdatedAt?.Ticks ?? f.CreatedAt.Ticks}",
                Title = "Cơ sở đã được duyệt ✓",
                Message = $"Cơ sở '{f.Name}' đã được duyệt và đang mở đón khách.",
                Type = "facility",
                Url = "/Owner/Facilities",
                CreatedAt = f.UpdatedAt ?? f.CreatedAt
            });
            notifications.AddRange(approvedFacilities);
        }

        var sorted = notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(12)
            .Select(n =>
            {
                n.TimeAgo = FormatTimeAgo(n.CreatedAt);
                return n;
            })
            .ToList();

        return Json(new NotificationResponseDto
        {
            Success = true,
            Items = sorted
        });
    }

    private static string FormatTimeAgo(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "Vừa xong";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
        return dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }
}
