using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OwnerReviewDto>> GetOwnerReviewsAsync(int ownerId, int? courtId = null)
    {
        var query = _context.Reviews
            .AsNoTracking()
            .Where(review => review.Court!.OwnerId == ownerId);

        if (courtId.HasValue)
        {
            query = query.Where(review => review.CourtId == courtId.Value);
        }

        return await query
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new OwnerReviewDto
            {
                Id = review.Id,
                BookingId = review.BookingId,
                CourtName = review.Court != null ? review.Court.Name : string.Empty,
                CustomerName = review.User != null ? review.User.FullName : string.Empty,
                BookingDate = review.Booking != null ? review.Booking.BookingDate : null,
                StartTime = review.Booking != null ? review.Booking.StartTime : null,
                EndTime = review.Booking != null ? review.Booking.EndTime : null,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                OwnerReply = review.OwnerReply,
                OwnerRepliedAt = review.OwnerRepliedAt
            })
            .ToListAsync();
    }

    public async Task<CustomerReviewFormDto?> GetCustomerReviewFormAsync(int bookingId, int userId)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(item => item.Court)
            .Include(item => item.Review)
            .SingleOrDefaultAsync(item => item.Id == bookingId && item.UserId == userId);

        if (booking is null
            || !ReviewRules.CanReview(booking.Status, booking.BookingDate, booking.EndTime, VietnamClock.Now))
        {
            return null;
        }

        return new CustomerReviewFormDto
        {
            BookingId = booking.Id,
            CourtName = booking.Court?.Name ?? string.Empty,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Rating = booking.Review?.Rating ?? 5,
            Comment = booking.Review?.Comment,
            IsEditing = booking.Review is not null
        };
    }

    public async Task<(bool Success, string Message)> SaveCustomerReviewAsync(
        int bookingId,
        int userId,
        CustomerReviewRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            return (false, "Vui lòng chọn số sao từ 1 đến 5.");
        }

        var comment = request.Comment?.Trim() ?? string.Empty;
        if (comment.Length > 500)
        {
            return (false, "Nhận xét không được vượt quá 500 ký tự.");
        }

        await using var transaction = await BeginBookingLockAsync(bookingId);
        var booking = await _context.Bookings
            .Include(item => item.Review)
            .SingleOrDefaultAsync(item => item.Id == bookingId && item.UserId == userId);

        if (booking is null)
        {
            return (false, "Không tìm thấy booking thuộc tài khoản của bạn.");
        }

        if (!ReviewRules.CanReview(booking.Status, booking.BookingDate, booking.EndTime, VietnamClock.Now))
        {
            return (false, "Chỉ có thể đánh giá sau khi khung giờ đặt sân đã kết thúc.");
        }

        var isEditing = booking.Review is not null;
        if (booking.Review is null)
        {
            booking.Review = new Review
            {
                BookingId = booking.Id,
                UserId = userId,
                CourtId = booking.CourtId,
                Rating = request.Rating,
                Comment = comment
            };
        }
        else
        {
            booking.Review.Rating = request.Rating;
            booking.Review.Comment = comment;
            booking.Review.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return (false, "Đánh giá đã được gửi ở một yêu cầu khác. Vui lòng tải lại trang.");
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return (true, isEditing ? "Đã cập nhật đánh giá." : "Cảm ơn bạn đã gửi đánh giá.");
    }

    public async Task<(bool Success, string Message)> ReplyToReviewAsync(
        int reviewId,
        int ownerId,
        string reply)
    {
        var normalizedReply = reply?.Trim() ?? string.Empty;
        if (normalizedReply.Length == 0 || normalizedReply.Length > 1000)
        {
            return (false, "Phản hồi phải từ 1 đến 1000 ký tự.");
        }

        var review = await _context.Reviews
            .SingleOrDefaultAsync(item => item.Id == reviewId && item.Court!.OwnerId == ownerId);
        if (review is null)
        {
            return (false, "Không tìm thấy đánh giá thuộc sân của bạn.");
        }

        review.OwnerReply = normalizedReply;
        review.OwnerRepliedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Đã lưu phản hồi.");
    }

    private async Task<IDbContextTransaction?> BeginBookingLockAsync(int bookingId)
    {
        if (!_context.Database.IsNpgsql())
        {
            return null;
        }

        var transaction = await _context.Database.BeginTransactionAsync();
        var lockKey = $"review-booking:{bookingId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");
        return transaction;
    }
}
