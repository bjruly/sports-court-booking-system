using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReviewItemDto>> GetAllReviewsAsync(string? search = null, string? filter = null)
    {
        var query = _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Court)
                .ThenInclude(c => c!.CourtType)
            .AsNoTracking();

        // Lọc theo trạng thái kiểm duyệt (UC26)
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.Trim().ToLowerInvariant();
            query = f switch
            {
                "reported" => query.Where(r => r.IsReported && !r.IsHidden),
                "hidden" => query.Where(r => r.IsHidden),
                "visible" => query.Where(r => !r.IsHidden),
                _ => query
            };
        }

        // Tìm kiếm theo tên khách, tên sân, nội dung
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r =>
                (r.User != null && r.User.FullName.ToLower().Contains(s)) ||
                (r.Court != null && r.Court.Name.ToLower().Contains(s)) ||
                r.Comment.ToLower().Contains(s));
        }

        var reviews = await query
            .OrderByDescending(r => r.IsReported && !r.IsHidden)
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => new ReviewItemDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User != null ? r.User.FullName : "Khách hàng",
                UserEmail = r.User != null ? r.User.Email : "",
                CourtId = r.CourtId,
                CourtName = r.Court != null ? r.Court.Name : "Sân thể thao",
                CourtTypeName = (r.Court != null && r.Court.CourtType != null) ? r.Court.CourtType.Name : "Thể thao",
                Rating = r.Rating,
                Comment = r.Comment,
                IsHidden = r.IsHidden,
                HiddenReason = r.HiddenReason,
                HiddenAt = r.HiddenAt,
                IsReported = r.IsReported,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return reviews;
    }

    public async Task<ReviewStatsSummaryDto> GetReviewStatsAsync()
    {
        var all = await _context.Reviews.AsNoTracking().ToListAsync();
        return new ReviewStatsSummaryDto
        {
            TotalReviews = all.Count,
            VisibleReviews = all.Count(r => !r.IsHidden),
            ReportedReviews = all.Count(r => r.IsReported && !r.IsHidden),
            HiddenReviews = all.Count(r => r.IsHidden)
        };
    }

    public async Task<(bool Success, string Message)> HideReviewAsync(int reviewId, string reason)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null)
        {
            return (false, "Không tìm thấy đánh giá cần xử lý.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return (false, "Vui lòng nhập lý do ẩn đánh giá vi phạm.");
        }

        review.IsHidden = true;
        review.HiddenReason = reason.Trim();
        review.HiddenAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Đã ẩn đánh giá vi phạm thành công.");
    }

    public async Task<(bool Success, string Message)> UnhideReviewAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null)
        {
            return (false, "Không tìm thấy đánh giá cần xử lý.");
        }

        review.IsHidden = false;
        review.HiddenReason = null;
        review.HiddenAt = null;
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Đã khôi phục hiển thị đánh giá thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null)
        {
            return (false, "Không tìm thấy đánh giá cần xóa.");
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return (true, "Đã xóa vĩnh viễn đánh giá thành công.");
    }
}
