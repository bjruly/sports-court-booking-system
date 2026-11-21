using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class OwnerVoucherService : IVoucherService
{
    private static readonly Regex ValidCodePattern = new(@"^[A-Z0-9][A-Z0-9_-]{2,29}$", RegexOptions.Compiled);
    private readonly AppDbContext _context;

    public OwnerVoucherService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OwnerVoucherDto>> GetOwnerVouchersAsync(int ownerId)
    {
        var now = DateTime.UtcNow;
        return await _context.Vouchers
            .AsNoTracking()
            .Where(voucher => voucher.OwnerId == ownerId)
            .OrderByDescending(voucher => voucher.CreatedAt)
            .Select(voucher => new OwnerVoucherDto
            {
                Id = voucher.Id,
                Code = voucher.Code,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                UsageLimit = voucher.UsageLimit,
                UsedCount = voucher.Bookings.Count(booking =>
                    booking.Status != BookingStatus.Cancelled
                    && booking.Status != BookingStatus.Expired
                    && (booking.Status != BookingStatus.Pending
                        || !booking.ExpiresAt.HasValue
                        || booking.ExpiresAt.Value > now)),
                IsActive = voucher.IsActive
            })
            .ToListAsync();
    }

    public async Task<OwnerVoucherDto?> GetOwnerVoucherAsync(int voucherId, int ownerId)
    {
        var now = DateTime.UtcNow;
        return await _context.Vouchers
            .AsNoTracking()
            .Where(voucher => voucher.Id == voucherId && voucher.OwnerId == ownerId)
            .Select(voucher => new OwnerVoucherDto
            {
                Id = voucher.Id,
                Code = voucher.Code,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                UsageLimit = voucher.UsageLimit,
                UsedCount = voucher.Bookings.Count(booking =>
                    booking.Status != BookingStatus.Cancelled
                    && booking.Status != BookingStatus.Expired
                    && (booking.Status != BookingStatus.Pending
                        || !booking.ExpiresAt.HasValue
                        || booking.ExpiresAt.Value > now)),
                IsActive = voucher.IsActive
            })
            .SingleOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> CreateOwnerVoucherAsync(
        int ownerId,
        OwnerVoucherRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        if (!await IsOwnerAsync(ownerId))
        {
            return (false, "Tài khoản chủ sân không hợp lệ.");
        }

        var code = NormalizeCode(request.Code);
        if (await HasCodeAsync(ownerId, code))
        {
            return (false, "Mã voucher này đã được sử dụng trong tài khoản của bạn.");
        }

        var voucher = new Voucher
        {
            OwnerId = ownerId,
            Code = code,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            StartDate = request.StartDate!.Value,
            EndDate = request.EndDate!.Value,
            UsageLimit = request.UsageLimit,
            IsActive = request.IsActive
        };
        _context.Vouchers.Add(voucher);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return (false, "Mã voucher này đã được sử dụng trong tài khoản của bạn.");
        }

        return (true, "Đã tạo voucher.");
    }

    public async Task<(bool Success, string Message)> UpdateOwnerVoucherAsync(
        int voucherId,
        int ownerId,
        OwnerVoucherRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        await using var transaction = await BeginVoucherLockAsync(voucherId);
        var voucher = await _context.Vouchers
            .SingleOrDefaultAsync(item => item.Id == voucherId && item.OwnerId == ownerId);
        if (voucher is null)
        {
            return (false, "Không tìm thấy voucher thuộc tài khoản của bạn.");
        }

        var code = NormalizeCode(request.Code);
        if (await HasCodeAsync(ownerId, code, voucherId))
        {
            return (false, "Mã voucher này đã được sử dụng trong tài khoản của bạn.");
        }

        var usedCount = await GetUsedCountAsync(voucherId);
        if (request.UsageLimit.HasValue && request.UsageLimit.Value < usedCount)
        {
            return (false, $"Giới hạn lượt dùng không được thấp hơn số lượt đang được tính ({usedCount}).");
        }

        voucher.Code = code;
        voucher.DiscountType = request.DiscountType;
        voucher.DiscountValue = request.DiscountValue;
        voucher.StartDate = request.StartDate!.Value;
        voucher.EndDate = request.EndDate!.Value;
        voucher.UsageLimit = request.UsageLimit;
        voucher.IsActive = request.IsActive;
        voucher.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return (false, "Mã voucher này đã được sử dụng trong tài khoản của bạn.");
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return (true, "Đã cập nhật voucher.");
    }

    public async Task<(bool Success, string Message, bool? IsActive)> ToggleOwnerVoucherAsync(
        int voucherId,
        int ownerId)
    {
        await using var transaction = await BeginVoucherLockAsync(voucherId);
        var voucher = await _context.Vouchers
            .SingleOrDefaultAsync(item => item.Id == voucherId && item.OwnerId == ownerId);
        if (voucher is null)
        {
            return (false, "Không tìm thấy voucher thuộc tài khoản của bạn.", null);
        }

        voucher.IsActive = !voucher.IsActive;
        voucher.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return (true, voucher.IsActive ? "Đã kích hoạt voucher." : "Đã ngừng áp dụng voucher.", voucher.IsActive);
    }

    public async Task<VoucherApplicationDto> ValidateForCourtAsync(
        int courtId,
        DateOnly applicationDate,
        decimal subtotal,
        string code)
    {
        var normalizedCode = NormalizeCode(code);
        var failure = new VoucherApplicationDto
        {
            Success = false,
            Code = normalizedCode,
            TotalPrice = subtotal
        };

        if (!ValidCodePattern.IsMatch(normalizedCode))
        {
            failure.Message = "Mã voucher không hợp lệ.";
            return failure;
        }

        var ownerId = await _context.Courts
            .Where(court => court.Id == courtId)
            .Select(court => (int?)court.OwnerId)
            .SingleOrDefaultAsync();
        if (!ownerId.HasValue)
        {
            failure.Message = "Không tìm thấy sân.";
            return failure;
        }

        var voucher = await _context.Vouchers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.OwnerId == ownerId.Value && item.Code == normalizedCode);
        if (voucher is null)
        {
            failure.Message = "Mã voucher không tồn tại hoặc không áp dụng cho sân này.";
            return failure;
        }

        var usedCount = await GetUsedCountAsync(voucher.Id);
        var validationError = VoucherRules.ValidateApplication(voucher, applicationDate, usedCount);
        if (validationError is not null)
        {
            failure.Message = validationError;
            return failure;
        }

        var discount = VoucherRules.CalculateDiscount(subtotal, voucher);
        return new VoucherApplicationDto
        {
            Success = true,
            Message = "Áp dụng voucher thành công.",
            Code = voucher.Code,
            DiscountAmount = discount,
            TotalPrice = subtotal - discount
        };
    }

    private async Task<bool> IsOwnerAsync(int ownerId)
        => await _context.Users.AnyAsync(user => user.Id == ownerId && user.Role == UserRole.CourtOwner);

    private async Task<IDbContextTransaction?> BeginVoucherLockAsync(int voucherId)
    {
        if (!_context.Database.IsNpgsql())
        {
            return null;
        }

        var transaction = await _context.Database.BeginTransactionAsync();
        var lockKey = $"voucher:{voucherId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");
        return transaction;
    }

    private async Task<bool> HasCodeAsync(int ownerId, string code, int? excludedVoucherId = null)
        => await _context.Vouchers.AnyAsync(voucher =>
            voucher.OwnerId == ownerId
            && voucher.Code == code
            && (!excludedVoucherId.HasValue || voucher.Id != excludedVoucherId.Value));

    private async Task<int> GetUsedCountAsync(int voucherId)
    {
        var now = DateTime.UtcNow;
        return await _context.Bookings.CountAsync(booking =>
            booking.VoucherId == voucherId
            && booking.Status != BookingStatus.Cancelled
            && booking.Status != BookingStatus.Expired
            && (booking.Status != BookingStatus.Pending
                || !booking.ExpiresAt.HasValue
                || booking.ExpiresAt.Value > now));
    }

    private static string? ValidateRequest(OwnerVoucherRequest request)
    {
        var code = NormalizeCode(request.Code);
        if (!ValidCodePattern.IsMatch(code))
        {
            return "Mã chỉ gồm 3 đến 30 ký tự chữ, số, dấu gạch ngang hoặc gạch dưới.";
        }

        return VoucherRules.ValidateDefinition(
            request.DiscountType,
            request.DiscountValue,
            request.StartDate,
            request.EndDate,
            request.UsageLimit);
    }

    private static string NormalizeCode(string? code)
        => (code ?? string.Empty).Trim().ToUpperInvariant();
}
