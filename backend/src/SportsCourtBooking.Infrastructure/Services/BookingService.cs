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
using System.Text.RegularExpressions;

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
            CustomerName = b.User != null ? b.User.FullName : b.GuestName ?? string.Empty,
            CustomerPhone = b.User != null ? b.User.PhoneNumber : b.GuestPhone ?? string.Empty,
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
                CustomerName = booking.User != null ? booking.User.FullName : booking.GuestName ?? string.Empty,
                CustomerPhone = booking.User != null ? booking.User.PhoneNumber : booking.GuestPhone ?? string.Empty,
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

    public async Task<List<BookingDto>> GetOwnerBookingsForCalendarAsync(
        int ownerId,
        DateOnly startDate,
        DateOnly endDate,
        int? courtId = null)
    {
        await _expirationService.ExpireOverdueAsync();

        var query = _context.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Court!.OwnerId == ownerId &&
                booking.BookingDate >= startDate &&
                booking.BookingDate <= endDate);

        if (courtId.HasValue)
        {
            query = query.Where(booking => booking.CourtId == courtId.Value);
        }

        return await query
            .OrderBy(booking => booking.BookingDate)
            .ThenBy(booking => booking.StartTime)
            .Select(booking => new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                CustomerName = booking.User != null ? booking.User.FullName : booking.GuestName ?? string.Empty,
                CustomerPhone = booking.User != null ? booking.User.PhoneNumber : booking.GuestPhone ?? string.Empty,
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
                CustomerName = b.User != null ? b.User.FullName : b.GuestName ?? string.Empty,
                CustomerPhone = b.User != null ? b.User.PhoneNumber : b.GuestPhone ?? string.Empty,
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
        return await IsTimeSlotAvailableCoreAsync(courtId, date, startTime, endTime);
    }

    public Task<(bool Success, string Message, BookingDto? Data)> CreateBookingAsync(CreateBookingRequest request)
        => CreateBookingCoreAsync(
            request.UserId,
            ownerId: null,
            request.CourtId,
            request.BookingDate,
            request.StartTime,
            request.EndTime,
            request.Note,
            guestName: null,
            guestPhone: null,
            BookingStatus.Pending);

    public Task<(bool Success, string Message, BookingDto? Data)> CreateOwnerWalkInBookingAsync(
        int ownerId,
        OwnerWalkInBookingRequest request)
    {
        var guestName = request.GuestName?.Trim();
        if (string.IsNullOrWhiteSpace(guestName) || guestName.Length > 100)
        {
            return Task.FromResult<(bool Success, string Message, BookingDto? Data)>(
                (false, "Tên khách phải từ 1 đến 100 ký tự.", null));
        }

        var guestPhone = request.GuestPhone?.Trim();
        if (string.IsNullOrWhiteSpace(guestPhone)
            || guestPhone.Length > 20
            || !Regex.IsMatch(guestPhone, @"^(0|\+84)\d{9}$"))
        {
            return Task.FromResult<(bool Success, string Message, BookingDto? Data)>(
                (false, "Số điện thoại khách không hợp lệ (ví dụ 0901234567).", null));
        }

        return CreateBookingCoreAsync(
            userId: null,
            ownerId,
            request.CourtId,
            request.BookingDate,
            request.StartTime,
            request.EndTime,
            request.Note,
            guestName,
            guestPhone,
            BookingStatus.Confirmed);
    }

    private async Task<(bool Success, string Message, BookingDto? Data)> CreateBookingCoreAsync(
        int? userId,
        int? ownerId,
        int courtId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? note,
        string? guestName,
        string? guestPhone,
        BookingStatus status)
    {
        var rangeError = BookingRules.ValidateTimeRange(
            bookingDate, startTime, endTime, VietnamClock.Now);
        if (rangeError != null)
            return (false, rangeError, null);

        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note is { Length: > BookingRules.MaxNoteLength })
            return (false, $"Ghi chú tối đa {BookingRules.MaxNoteLength} ký tự.", null);

        if (userId.HasValue && !await _context.Users.AnyAsync(user => user.Id == userId.Value && user.IsActive))
        {
            return (false, "Tài khoản không hợp lệ hoặc đã bị khóa. Vui lòng đăng nhập lại.", null);
        }

        try
        {
            await _expirationService.ExpireOverdueAsync();
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var courtQuery = _context.Courts.Where(court => court.Id == courtId);
            if (ownerId.HasValue)
            {
                courtQuery = courtQuery.Where(court => court.OwnerId == ownerId.Value);
            }

            var court = await courtQuery.SingleOrDefaultAsync();
            if (court == null || court.Status != CourtStatus.Active)
            {
                return (false, "Sân không tồn tại, không thuộc quyền quản lý của bạn hoặc đang tạm ngưng hoạt động.", null);
            }

            // 2. Chống đặt trùng khi nhiều người bấm cùng lúc: khóa theo (sân, ngày) trong suốt transaction.
            //    Người đến sau sẽ đợi đến khi người trước commit xong rồi mới kiểm tra lại khung giờ.
            if (_context.Database.IsNpgsql())
            {
                // Chờ tối đa 10 giây để lấy khóa; quá hạn sẽ ném lỗi và được bắt ở khối catch bên dưới.
                await _context.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '10s'");
                var lockKey = $"booking:{court.Id}:{bookingDate:yyyyMMdd}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");
            }

            var isAvailable = await IsTimeSlotAvailableCoreAsync(courtId, bookingDate, startTime, endTime);
            if (!isAvailable)
                return (false, "Khung giờ này đã có người đặt. Vui lòng chọn khung giờ khác!", null);

            // 3. Giá = tổng giá từng khung giờ (đã gồm phụ thu giờ cao điểm), cùng công thức với giao diện
            var createdAt = DateTime.UtcNow;
            var booking = new Booking
            {
                UserId = userId,
                GuestName = guestName,
                GuestPhone = guestPhone,
                CourtId = courtId,
                BookingDate = bookingDate,
                StartTime = startTime,
                EndTime = endTime,
                TotalPrice = BookingRules.CalculateTotalPrice(court.PricePerHour, startTime, endTime),
                Note = note,
                Status = status,
                CreatedAt = createdAt,
                ExpiresAt = status == BookingStatus.Pending
                    ? createdAt.AddMinutes(Booking.PendingExpirationMinutes)
                    : null
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = status == BookingStatus.Confirmed
                ? "Đã tạo và xác nhận đơn đặt sân."
                : "Đã tạo đơn đặt sân, đang chờ xác nhận.";

            return (true, message, new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                CustomerName = guestName ?? string.Empty,
                CustomerPhone = guestPhone ?? string.Empty,
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
                courtId,
                bookingDate);
            return (false, "Khung giờ này đã có người đặt. Vui lòng chọn khung giờ khác!", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tạo booking cho sân {CourtId} ngày {Date}", courtId, bookingDate);
            return (false, "Hệ thống đang bận hoặc gặp lỗi khi lưu đơn đặt sân. Vui lòng thử lại.", null);
        }
    }

    private async Task<bool> IsTimeSlotAvailableCoreAsync(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var isOverlapping = await _context.Bookings.AnyAsync(booking =>
            booking.CourtId == courtId &&
            booking.BookingDate == date &&
            (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed) &&
            startTime < booking.EndTime &&
            endTime > booking.StartTime);

        return !isOverlapping;
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
                : b.GuestName ?? string.Empty,

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
