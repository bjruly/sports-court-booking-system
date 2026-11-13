using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingService> _logger;

    public BookingService(AppDbContext context, ILogger<BookingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BookingDto>> GetBookingsByDateAsync(DateOnly date, int? courtId = null)
    {
        var query = _context.Bookings
            .Where(b => b.BookingDate == date && b.Status != BookingStatus.Cancelled);

        if (courtId.HasValue)
        {
            query = query.Where(b => b.CourtId == courtId.Value);
        }

        return await query.OrderBy(b => b.StartTime).Select(b => new BookingDto
        {
            Id = b.Id,
            UserId = b.UserId,
            CustomerName = b.User != null ? b.User.FullName : string.Empty,
            CourtId = b.CourtId,
            CourtName = b.Court != null ? b.Court.Name : string.Empty,
            BookingDate = b.BookingDate,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            TotalPrice = b.TotalPrice,
            Status = b.Status
        }).ToListAsync();
    }

    public async Task<List<BookingDto>> GetBookingsByUserAsync(int userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                UserId = b.UserId,
                CustomerName = b.User != null ? b.User.FullName : string.Empty,
                CourtId = b.CourtId,
                CourtName = b.Court != null ? b.Court.Name : string.Empty,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalPrice = b.TotalPrice,
                Status = b.Status
            }).ToListAsync();
    }

    public async Task<bool> IsTimeSlotAvailableAsync(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        // Hai khoảng thời gian [StartA, EndA] và [StartB, EndB] giao nhau khi: StartA < EndB VÀ EndA > StartB
        bool isOverlapping = await _context.Bookings.AnyAsync(b =>
            b.CourtId == courtId &&
            b.BookingDate == date &&
            b.Status != BookingStatus.Cancelled &&
            startTime < b.EndTime &&
            endTime > b.StartTime);

        return !isOverlapping;
    }

    public async Task<(bool Success, string Message, BookingDto? Data)> CreateBookingAsync(CreateBookingRequest request)
    {
        // 1. Kiểm tra dữ liệu đầu vào (áp dụng cho mọi nơi gọi, không chỉ riêng giao diện web)
        var rangeError = BookingRules.ValidateTimeRange(
            request.BookingDate, request.StartTime, request.EndTime, VietnamClock.Now);
        if (rangeError != null)
            return (false, rangeError, null);

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note != null && note.Length > BookingRules.MaxNoteLength)
            return (false, $"Ghi chú tối đa {BookingRules.MaxNoteLength} ký tự.", null);

        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId && u.IsActive);
        if (!userExists)
            return (false, "Tài khoản không hợp lệ hoặc đã bị khóa. Vui lòng đăng nhập lại.", null);

        var court = await _context.Courts.FindAsync(request.CourtId);
        if (court == null || court.Status != CourtStatus.Active)
            return (false, "Sân không tồn tại hoặc đang tạm ngưng hoạt động.", null);

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 2. Chống đặt trùng khi nhiều người bấm cùng lúc: khóa theo (sân, ngày) trong suốt transaction.
            //    Người đến sau sẽ đợi đến khi người trước commit xong rồi mới kiểm tra lại khung giờ.
            if (_context.Database.IsNpgsql())
            {
                // Chờ tối đa 10 giây để lấy khóa; quá hạn sẽ ném lỗi và được bắt ở khối catch bên dưới.
                await _context.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '10s'");
                var lockKey = $"booking:{court.Id}:{request.BookingDate:yyyyMMdd}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");
            }

            bool isAvailable = await IsTimeSlotAvailableAsync(
                request.CourtId, request.BookingDate, request.StartTime, request.EndTime);
            if (!isAvailable)
                return (false, "Khung giờ này đã có người đặt. Vui lòng chọn khung giờ khác!", null);

            // 3. Giá = tổng giá từng khung giờ (đã gồm phụ thu giờ cao điểm), cùng công thức với giao diện
            var booking = new Booking
            {
                UserId = request.UserId,
                CourtId = request.CourtId,
                BookingDate = request.BookingDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                TotalPrice = BookingRules.CalculateTotalPrice(court.PricePerHour, request.StartTime, request.EndTime),
                Note = note,
                Status = BookingStatus.Pending // MVP: đơn mới ở trạng thái chờ xác nhận
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Đã tạo đơn đặt sân, đang chờ xác nhận.", new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                CourtId = booking.CourtId,
                CourtName = court.Name,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tạo booking cho sân {CourtId} ngày {Date}", request.CourtId, request.BookingDate);
            return (false, "Hệ thống đang bận hoặc gặp lỗi khi lưu đơn đặt sân. Vui lòng thử lại.", null);
        }
    }
}
