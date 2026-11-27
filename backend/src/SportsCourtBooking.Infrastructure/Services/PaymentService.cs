using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

/// <summary>
/// Demo-only payment/refund workflow. No money is transferred by these methods.
/// A provider adapter and verified callbacks are required for real payments.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> CompleteDemoPaymentAsync(
        int bookingId,
        int userId,
        PaymentMethodType method)
    {
        if (!Enum.IsDefined(method))
        {
            return (false, "Phương thức thanh toán không hợp lệ.");
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        var booking = await LockBookingAsync(bookingId);
        if (booking is null || booking.UserId != userId)
        {
            return (false, "Không tìm thấy đơn đặt sân của bạn.");
        }

        await _context.Entry(booking).Reference(item => item.Payment).LoadAsync();
        if (booking.Payment is null)
        {
            return (false, "Đơn đặt sân chưa có bản ghi thanh toán. Vui lòng liên hệ quản trị viên.");
        }

        if (PaymentStatusRules.IsPaid(booking.Payment.Status)
            && booking.Status == BookingStatus.Confirmed)
        {
            return (true, "Đơn này đã được thanh toán.");
        }

        var now = DateTime.UtcNow;
        if (booking.Status != BookingStatus.Pending
            || !booking.ExpiresAt.HasValue
            || booking.ExpiresAt.Value <= now
            || booking.Payment.Status != PaymentStatus.Unpaid)
        {
            return (false, "Đơn đã hết hạn hoặc không còn ở trạng thái chờ thanh toán.");
        }

        booking.Payment.PaymentMethod = method.ToString();
        booking.Payment.Status = PaymentStatus.DemoPaid;
        booking.Payment.TransactionCode = $"DEMO-PAY-{Guid.NewGuid():N}";
        booking.Payment.PaidAt = now;
        booking.Payment.UpdatedAt = now;
        booking.Status = BookingStatus.Confirmed;
        booking.ExpiresAt = null;
        booking.UpdatedAt = now;

        await _context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        _logger.LogInformation("Demo payment recorded for booking {BookingId}", bookingId);
        return (true, "Thanh toán thành công.");
    }

    public async Task<(bool Success, string Message)> RequestRefundAsync(
        int bookingId,
        int userId,
        string reason)
    {
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length == 0 || reason.Length > 200)
        {
            return (false, "Lý do hoàn tiền phải từ 1 đến 200 ký tự.");
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        var booking = await LockBookingAsync(bookingId);
        if (booking is null || booking.UserId != userId)
        {
            return (false, "Không tìm thấy đơn đặt sân của bạn.");
        }

        await _context.Entry(booking).Reference(item => item.Payment).LoadAsync();
        if (booking.Payment is null)
        {
            return (false, "Đơn này chưa có thanh toán để hoàn.");
        }

        await _context.Entry(booking.Payment).Reference(payment => payment.Refund).LoadAsync();
        if (booking.Payment.Refund is not null)
        {
            return (false, "Đơn này đã có yêu cầu hoàn tiền.");
        }

        var paid = PaymentStatusRules.IsPaid(booking.Payment.Status);
        var startsAtVietnam = booking.BookingDate.ToDateTime(booking.StartTime);
        if (!paid
            || booking.Status != BookingStatus.Confirmed
            || booking.CheckInAt.HasValue
            || startsAtVietnam <= VietnamClock.Now)
        {
            return (false, "Chỉ có thể yêu cầu hoàn tiền cho đơn đã thanh toán và chưa bắt đầu.");
        }

        var now = DateTime.UtcNow;
        var refund = new Refund
        {
            PaymentId = booking.Payment.Id,
            Amount = booking.Payment.Amount,
            Reason = reason,
            Status = RefundStatus.Pending,
            RequestedAt = now,
            RequestedByUserId = userId,
            CreatedAt = now
        };

        _context.Refunds.Add(refund);
        booking.Status = BookingStatus.Cancelled;
        booking.ExpiresAt = null;
        booking.UpdatedAt = now;

        await _context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return (true, "Đã gửi yêu cầu hoàn tiền. Chủ sân sẽ xử lý yêu cầu của bạn.");
    }

    public Task<List<RefundDto>> GetAdminRefundsAsync()
        => ProjectRefunds(_context.Refunds.AsNoTracking())
            .OrderByDescending(refund => refund.RequestedAt)
            .ToListAsync();

    public Task<List<RefundDto>> GetOwnerRefundsAsync(int ownerId)
        => ProjectRefunds(_context.Refunds.AsNoTracking()
                .Where(refund => refund.Payment!.Booking!.Court!.OwnerId == ownerId))
            .OrderByDescending(refund => refund.RequestedAt)
            .ToListAsync();

    public async Task<(bool Success, string Message)> ProcessDemoRefundAsync(
        int refundId,
        int ownerId,
        int actorUserId,
        bool succeeded)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        var refund = await LockRefundAsync(refundId);
        if (refund is null || !await IsOwnerRefundAsync(refund.PaymentId, ownerId))
        {
            return (false, "Không tìm thấy yêu cầu hoàn tiền thuộc sân của bạn.");
        }

        await _context.Entry(refund).Reference(item => item.Payment).LoadAsync();
        if (refund.Status != RefundStatus.Pending || refund.Payment is null
            || !PaymentStatusRules.IsPaid(refund.Payment.Status))
        {
            return (false, "Yêu cầu này không còn ở trạng thái chờ xử lý.");
        }

        var now = DateTime.UtcNow;
        refund.Status = succeeded ? RefundStatus.Completed : RefundStatus.Failed;
        refund.ProcessedAt = now;
        refund.ProcessedByUserId = actorUserId;
        refund.TransactionCode = $"DEMO-REF-{Guid.NewGuid():N}";
        refund.UpdatedAt = now;
        if (succeeded)
        {
            refund.Payment.Status = refund.Payment.Status == PaymentStatus.DemoPaid
                ? PaymentStatus.DemoRefunded
                : PaymentStatus.Refunded;
            refund.Payment.UpdatedAt = now;
        }

        _context.RecordActivity(UserRole.CourtOwner, actorUserId, succeeded ? "Ghi nhận hoàn tiền thành công" : "Ghi nhận hoàn tiền thất bại",
            $"Hoàn tiền #{refund.Id} · Đơn #{refund.Payment.BookingId}");
        await _context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        _logger.LogInformation(
            "Owner {OwnerId} recorded demo refund result {Succeeded} for refund {RefundId}",
            ownerId, succeeded, refundId);
        return succeeded
            ? (true, "Đã ghi nhận hoàn tiền thành công.")
            : (true, "Giao dịch hoàn tiền thất bại. Bạn có thể nhập mã giao dịch sau khi hoàn thủ công hoặc từ chối yêu cầu.");
    }

    public async Task<(bool Success, string Message)> CompleteOwnerRefundManuallyAsync(
        int refundId,
        int ownerId,
        int actorUserId,
        string transactionCode)
    {
        transactionCode = (transactionCode ?? string.Empty).Trim();
        if (transactionCode.Length == 0 || transactionCode.Length > 100)
        {
            return (false, "Mã giao dịch hoàn tiền phải từ 1 đến 100 ký tự.");
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        var refund = await LockRefundAsync(refundId);
        if (refund is null || !await IsOwnerRefundAsync(refund.PaymentId, ownerId))
        {
            return (false, "Không tìm thấy yêu cầu hoàn tiền thuộc sân của bạn.");
        }

        await _context.Entry(refund).Reference(item => item.Payment).LoadAsync();
        if (refund.Status != RefundStatus.Failed || refund.Payment is null)
        {
            return (false, "Chỉ có thể xác nhận thủ công yêu cầu hoàn tiền đã thất bại.");
        }

        var now = DateTime.UtcNow;
        refund.Status = RefundStatus.Completed;
        refund.ProcessedAt = now;
        refund.ProcessedByUserId = actorUserId;
        refund.TransactionCode = transactionCode;
        refund.UpdatedAt = now;
        refund.Payment.Status = refund.Payment.Status == PaymentStatus.DemoPaid
            ? PaymentStatus.DemoRefunded
            : PaymentStatus.Refunded;
        refund.Payment.UpdatedAt = now;

        _context.RecordActivity(UserRole.CourtOwner, actorUserId, "Xác nhận hoàn tiền thủ công",
            $"Hoàn tiền #{refund.Id} · Đơn #{refund.Payment.BookingId}");
        await _context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return (true, "Đã ghi nhận hoàn tiền thủ công hoàn tất.");
    }

    public async Task<(bool Success, string Message)> RejectOwnerRefundAsync(
        int refundId,
        int ownerId,
        int actorUserId,
        string reason)
    {
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length == 0 || reason.Length > 500)
        {
            return (false, "Vui lòng nhập lý do từ chối (tối đa 500 ký tự).");
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        var refund = await LockRefundAsync(refundId);
        if (refund is null || !await IsOwnerRefundAsync(refund.PaymentId, ownerId))
        {
            return (false, "Không tìm thấy yêu cầu hoàn tiền thuộc sân của bạn.");
        }

        if (refund.Status != RefundStatus.Failed)
        {
            return (false, "Chỉ có thể từ chối yêu cầu đã xử lý thất bại.");
        }

        var now = DateTime.UtcNow;
        refund.Status = RefundStatus.Rejected;
        refund.RejectionReason = reason;
        refund.ProcessedAt = now;
        refund.ProcessedByUserId = actorUserId;
        refund.UpdatedAt = now;

        _context.RecordActivity(UserRole.CourtOwner, actorUserId, "Từ chối yêu cầu hoàn tiền",
            $"Hoàn tiền #{refund.Id}");
        await _context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return (true, "Đã từ chối yêu cầu hoàn tiền.");
    }

    private async Task<Booking?> LockBookingAsync(int bookingId)
    {
        if (_context.Database.IsNpgsql())
        {
            return await _context.Bookings
                .FromSqlInterpolated($"SELECT * FROM \"Bookings\" WHERE \"Id\" = {bookingId} FOR UPDATE")
                .SingleOrDefaultAsync();
        }

        return await _context.Bookings.SingleOrDefaultAsync(booking => booking.Id == bookingId);
    }

    private async Task<Refund?> LockRefundAsync(int refundId)
    {
        if (_context.Database.IsNpgsql())
        {
            return await _context.Refunds
                .FromSqlInterpolated($"SELECT * FROM \"Refunds\" WHERE \"Id\" = {refundId} FOR UPDATE")
                .SingleOrDefaultAsync();
        }

        return await _context.Refunds.SingleOrDefaultAsync(refund => refund.Id == refundId);
    }

    private Task<bool> IsOwnerRefundAsync(int paymentId, int ownerId)
        => _context.Payments.AnyAsync(payment => payment.Id == paymentId
            && payment.Booking!.Court!.OwnerId == ownerId);

    private static IQueryable<RefundDto> ProjectRefunds(IQueryable<Refund> refunds)
        => refunds.Select(refund => new RefundDto
        {
            Id = refund.Id,
            BookingId = refund.Payment!.BookingId,
            PaymentId = refund.PaymentId,
            CustomerName = refund.Payment.Booking!.User != null
                ? refund.Payment.Booking.User.FullName
                : refund.Payment.Booking.GuestName ?? "Khách vãng lai",
            CourtName = refund.Payment.Booking.Court!.Name,
            BookingDate = refund.Payment.Booking.BookingDate,
            Amount = refund.Amount,
            Reason = refund.Reason,
            Status = refund.Status,
            PaymentStatus = refund.Payment.Status,
            RequestedAt = refund.RequestedAt,
            ProcessedAt = refund.ProcessedAt,
            TransactionCode = refund.TransactionCode,
            RejectionReason = refund.RejectionReason
        });
}
