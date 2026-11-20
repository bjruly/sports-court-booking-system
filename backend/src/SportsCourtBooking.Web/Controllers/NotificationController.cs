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

    // Danh sách tên các cơ sở mẫu/seed ban đầu của hệ thống
    private static readonly string[] SeedFacilityNames = new[]
    {
        "Cơ sở Cầu Lông Nam Sài Gòn - Quận 7",
        "Cơ sở Cầu Lông Chu Văn An - Bình Thạnh",
        "Cụm Sân Bóng Đá Mini Thủ Đức",
        "Trung Tâm Pickleball Quận 10",
        "CLB Tennis Thảo Điền - Quận 2",
        "CLB Cầu Lông Sân Nhà Quận 7",
        "Sân Cầu Lông Sân Nhà Bình Thạnh",
        "Chảo Lửa Mini Sân Nhà Thủ Đức",
        "Pickleball Sân Nhà Quận 10",
        "Sân Quần Vợt Sân Nhà Quận 2",
        "Cụm Sân Thể Thao Rạch Miễu",
        "Sân Thể Thao Tạm Khóa VIP"
    };

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

        // Lọc bỏ toàn bộ dữ liệu mẫu / seed ban đầu:
        // 1. Chỉ lấy các sự kiện phát sinh gần đây (trong vòng 14 ngày qua)
        // 2. Không lấy dữ liệu mẫu từ các ngày seed hoặc cơ sở mặc định
        var seedCutoff = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var recentThreshold = DateTime.UtcNow.AddDays(-14);
        var effectiveCutoff = recentThreshold > seedCutoff ? recentThreshold : seedCutoff;

        if (isAdmin)
        {
            // 1. Cơ sở do người dùng thật đăng ký mới chờ Admin duyệt (loại trừ cơ sở mẫu)
            var pendingFacilitiesRaw = await _context.Facilities
                .AsNoTracking()
                .Include(f => f.Owner)
                .Where(f => !f.IsApproved 
                         && !SeedFacilityNames.Contains(f.Name) 
                         && f.CreatedAt >= effectiveCutoff)
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

            // 2. Đơn đặt sân mới phát sinh thật từ khách hàng (loại trừ đơn mẫu)
            var recentBookingsRaw = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Court)
                .Where(b => b.CreatedAt >= effectiveCutoff)
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

            // 3. Đánh giá mới nhất từ người dùng thật (loại trừ đánh giá mẫu hoặc đã bị ẩn)
            var recentReviewsRaw = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Court)
                .Where(r => r.CreatedAt >= effectiveCutoff && !r.IsHidden)
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
            // 1. Đơn đặt sân mới phát sinh thật trên các sân thuộc quyền quản lý của Owner
            var ownerBookingsRaw = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Court)
                .Where(b => b.Court != null && b.Court.OwnerId == userId.Value && b.CreatedAt >= effectiveCutoff)
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

            // 2. Đánh giá mới phát sinh trên các sân của Owner (loại trừ đánh giá mẫu)
            var ownerReviewsRaw = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Court)
                .Where(r => r.Court != null && r.Court.OwnerId == userId.Value && r.CreatedAt >= effectiveCutoff && !r.IsHidden)
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

            // 3. Cơ sở mới do chính Owner này tạo đã được Admin phê duyệt (loại trừ 5 cơ sở mẫu)
            var approvedFacilitiesRaw = await _context.Facilities
                .AsNoTracking()
                .Where(f => f.OwnerId == userId.Value 
                         && f.IsApproved 
                         && f.UpdatedAt != null 
                         && f.UpdatedAt >= effectiveCutoff 
                         && !SeedFacilityNames.Contains(f.Name))
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
