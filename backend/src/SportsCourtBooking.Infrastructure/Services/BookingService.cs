using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using Npgsql;

namespace SportsCourtBooking.Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingService> _logger;
    private readonly PendingBookingExpirationService _expirationService;

    public BookingService(
        AppDbContext context,
        ILogger<BookingService> logger,
        PendingBookingExpirationService expirationService)
    {
        _context = context;
        _logger = logger;
        _expirationService = expirationService;
    }

    public async Task<List<BookingDto>> GetBookingsByDateAsync(DateOnly date, int? courtId = null, bool includeInactive = false)
    {
        await _expirationService.ExpireOverdueAsync();

        var query = _context.Bookings.Where(b => b.BookingDate == date);

        if (!includeInactive)
        {
            // Đơn đã hủy hoặc hết hạn không còn giữ khung giờ nên không được hiện là "đã đặt".
            query = query.Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Expired);
        }

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

    public async Task<List<BookingDto>> GetOwnerBookingsAsync(
        int ownerId,
        DateOnly? date = null,
        int? courtId = null,
        BookingStatus? status = null)
    {
        await _expirationService.ExpireOverdueAsync();

        var query = _context.Bookings
            .AsNoTracking()
            .Where(booking => booking.Court!.OwnerId == ownerId);

        if (date.HasValue)
        {
            query = query.Where(booking => booking.BookingDate == date.Value);
        }

        if (courtId.HasValue)
        {
            query = query.Where(booking => booking.CourtId == courtId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(booking => booking.Status == status.Value);
        }

        return await query
            .OrderByDescending(booking => booking.BookingDate)
            .ThenBy(booking => booking.StartTime)
            .Select(booking => new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                CustomerName = booking.User != null ? booking.User.FullName : string.Empty,
                CourtId = booking.CourtId,
                CourtName = booking.Court != null ? booking.Court.Name : string.Empty,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status
            })
            .ToListAsync();
    }

    public async Task<List<BookingDto>> GetBookingsByUserAsync(int userId)
    {
        await _expirationService.ExpireOverdueAsync();

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
        await _expirationService.ExpireOverdueAsync();

        // Hai khoảng thời gian [StartA, EndA] và [StartB, EndB] giao nhau khi: StartA < EndB VÀ EndA > StartB
        bool isOverlapping = await _context.Bookings.AnyAsync(b =>
            b.CourtId == courtId &&
            b.BookingDate == date &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
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
            var createdAt = DateTime.UtcNow;
            var booking = new Booking
            {
                UserId = request.UserId,
                CourtId = request.CourtId,
                BookingDate = request.BookingDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                TotalPrice = BookingRules.CalculateTotalPrice(court.PricePerHour, request.StartTime, request.EndTime),
                Note = note,
                Status = BookingStatus.Pending,
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddMinutes(Booking.PendingExpirationMinutes)
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
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            _logger.LogInformation(
                ex,
                "Booking bị từ chối do trùng khung giờ ở sân {CourtId} ngày {Date}",
                request.CourtId,
                request.BookingDate);
            return (false, "Khung giờ này đã có người đặt. Vui lòng chọn khung giờ khác!", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tạo booking cho sân {CourtId} ngày {Date}", request.CourtId, request.BookingDate);
            return (false, "Hệ thống đang bận hoặc gặp lỗi khi lưu đơn đặt sân. Vui lòng thử lại.", null);
        }
    }

    public Task<(bool Success, string Message)> ConfirmBookingAsync(int bookingId, int actorUserId)
        => ConfirmBookingScopedAsync(bookingId, actorUserId, null);

    public Task<(bool Success, string Message)> ConfirmOwnerBookingAsync(int bookingId, int ownerId, int actorUserId)
        => ConfirmBookingScopedAsync(bookingId, actorUserId, ownerId);

    private async Task<(bool Success, string Message)> ConfirmBookingScopedAsync(
        int bookingId,
        int actorUserId,
        int? ownerId)
    {
        await _expirationService.ExpireOverdueAsync();

        var now = DateTime.UtcNow;
        var bookingScope = GetBookingScope(bookingId, ownerId);

        // Một câu UPDATE có điều kiện: chỉ đổi khi đơn vẫn Pending và còn hạn, nên không tranh chấp với tác vụ hết hạn.
        var updated = await bookingScope
            .Where(b => b.Status == BookingStatus.Pending
                && (b.ExpiresAt == null || b.ExpiresAt > now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Status, BookingStatus.Confirmed)
                .SetProperty(b => b.ExpiresAt, (DateTime?)null)
                .SetProperty(b => b.UpdatedAt, now));

        if (updated == 0)
        {
            var current = await GetStatusAsync(bookingId, ownerId);
            _logger.LogWarning(
                "Người dùng {ActorUserId} xác nhận đơn {BookingId} thất bại, trạng thái hiện tại: {Status}",
                actorUserId, bookingId, current);

            return (false, current switch
            {
                null => ownerId.HasValue ? "Không tìm thấy đơn đặt sân thuộc sân của bạn." : "Không tìm thấy đơn đặt sân.",
                BookingStatus.Expired => "Đơn đã hết hạn giữ chỗ nên không thể xác nhận.",
                BookingStatus.Confirmed => "Đơn này đã được xác nhận trước đó.",
                _ => "Chỉ đơn đang chờ xác nhận mới có thể xác nhận."
            });
        }

        _logger.LogInformation("Người dùng {ActorUserId} đã xác nhận đơn {BookingId}", actorUserId, bookingId);
        return (true, "Đã xác nhận đơn đặt sân.");
    }

    public Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, int actorUserId)
        => CancelBookingScopedAsync(bookingId, actorUserId, null);

    public Task<(bool Success, string Message)> CancelOwnerBookingAsync(int bookingId, int ownerId, int actorUserId)
        => CancelBookingScopedAsync(bookingId, actorUserId, ownerId);

    private async Task<(bool Success, string Message)> CancelBookingScopedAsync(
        int bookingId,
        int actorUserId,
        int? ownerId)
    {
        await _expirationService.ExpireOverdueAsync();

        var now = DateTime.UtcNow;
        var bookingScope = GetBookingScope(bookingId, ownerId);

        var updated = await bookingScope
            .Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Status, BookingStatus.Cancelled)
                .SetProperty(b => b.UpdatedAt, now));

        if (updated == 0)
        {
            var current = await GetStatusAsync(bookingId, ownerId);
            _logger.LogWarning(
                "Người dùng {ActorUserId} hủy đơn {BookingId} thất bại, trạng thái hiện tại: {Status}",
                actorUserId, bookingId, current);

            return (false, current switch
            {
                null => ownerId.HasValue ? "Không tìm thấy đơn đặt sân thuộc sân của bạn." : "Không tìm thấy đơn đặt sân.",
                BookingStatus.Cancelled => "Đơn này đã được hủy trước đó.",
                _ => "Đơn ở trạng thái hiện tại không thể hủy."
            });
        }

        _logger.LogInformation("Người dùng {ActorUserId} đã hủy đơn {BookingId}, khung giờ được nhả", actorUserId, bookingId);
        return (true, "Đã hủy đơn đặt sân, khung giờ được mở lại.");
    }

    private IQueryable<Booking> GetBookingScope(int bookingId, int? ownerId)
    {
        var query = _context.Bookings.Where(booking => booking.Id == bookingId);
        if (ownerId.HasValue)
        {
            var ownerCourtIds = _context.Courts
                .Where(court => court.OwnerId == ownerId.Value)
                .Select(court => court.Id);
            query = query.Where(booking => ownerCourtIds.Contains(booking.CourtId));
        }

        return query;
    }

public async Task<BookingDetailDto?> GetOwnBookingAsync(
    int bookingId,
    int userId)
{
    await _expirationService.ExpireOverdueAsync();

    return await _context.Bookings
        .AsNoTracking()
        .Where(b => b.Id == bookingId && b.UserId == userId)
        .Select(b => new BookingDetailDto
        {
            Id = b.Id,
            UserId = b.UserId,
            CourtId = b.CourtId,

            CustomerName = b.User != null
                ? b.User.FullName
                : string.Empty,

            CourtName = b.Court != null
                ? b.Court.Name
                : string.Empty,

            Location = b.Court != null
                ? b.Court.Location
                : string.Empty,

            BookingDate = b.BookingDate,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            TotalPrice = b.TotalPrice,
            Status = b.Status,
            Note = b.Note,
            CreatedAt = b.CreatedAt,
            ExpiresAt = b.ExpiresAt,

            IsPaid = b.Payment != null
                && b.Payment.Status == PaymentStatus.Paid
        })
        .FirstOrDefaultAsync();
}
    private Task<BookingStatus?> GetStatusAsync(int bookingId, int? ownerId)
        => GetBookingScope(bookingId, ownerId)
            .Select(b => (BookingStatus?)b.Status)
            .FirstOrDefaultAsync();
}
