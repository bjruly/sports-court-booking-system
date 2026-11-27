using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserDto>> GetCustomersAsync(string? search = null)
    {
        var query = _context.Users
            .Where(u => u.Role == UserRole.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword) ||
                u.PhoneNumber.Contains(keyword));
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<CourtOwnerItemDto>> GetCourtOwnersAsync(string? search = null, string? filter = null)
    {
        var query = _context.Users
            .Where(u => u.Role == UserRole.CourtOwner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword) ||
                u.PhoneNumber.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(filter) && !string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = filter.ToLower() switch
            {
                "active" => query.Where(u => u.IsActive),
                "locked" => query.Where(u => !u.IsActive),
                _ => query
            };
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new CourtOwnerItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                FacilityCount = _context.Facilities.Count(f => f.OwnerId == u.Id),
                CourtCount = _context.Courts.Count(c => c.OwnerId == u.Id),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CourtOwnerStatsDto> GetCourtOwnerStatsAsync()
    {
        var total = await _context.Users.CountAsync(u => u.Role == UserRole.CourtOwner);
        var active = await _context.Users.CountAsync(u => u.Role == UserRole.CourtOwner && u.IsActive);
        var locked = await _context.Users.CountAsync(u => u.Role == UserRole.CourtOwner && !u.IsActive);
        var totalFacilities = await _context.Facilities.CountAsync();

        return new CourtOwnerStatsDto
        {
            TotalOwners = total,
            ActiveOwners = active,
            LockedOwners = locked,
            TotalFacilities = totalFacilities
        };
    }

    public async Task<(bool Success, string Message, bool? IsActive)> ToggleLockOwnerAsync(int targetOwnerId, int? adminUserId)
    {
        if (adminUserId.HasValue && targetOwnerId == adminUserId.Value)
            return (false, "Không thể khóa chính tài khoản của mình.", null);

        var owner = await _context.Users.FindAsync(targetOwnerId);
        if (owner == null || owner.Role != UserRole.CourtOwner)
            return (false, "Không tìm thấy tài khoản Chủ sân.", null);

        owner.IsActive = !owner.IsActive;
        var action = owner.IsActive ? "Mở khóa" : "Khóa";
        _context.RecordAdminActivity(adminUserId, $"{action} tài khoản",
            $"Tài khoản #{owner.Id} · {owner.FullName}");
        await _context.SaveChangesAsync();

        return (true, $"{action} tài khoản Chủ sân \"{owner.FullName}\" thành công.", owner.IsActive);
    }

    public async Task<bool> IsUserActiveAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<(bool Success, string Message, bool? IsActive)> ToggleLockAsync(int targetUserId, int? adminUserId)
    {
        if (adminUserId.HasValue && targetUserId == adminUserId.Value)
            return (false, "Không thể khóa chính tài khoản của mình.", null);

        var user = await _context.Users.FindAsync(targetUserId);
        if (user == null)
            return (false, "Không tìm thấy người dùng.", null);

        if (user.Role == UserRole.Admin)
            return (false, "Không thể khóa tài khoản Admin.", null);

        user.IsActive = !user.IsActive;
        var action = user.IsActive ? "Mở khóa" : "Khóa";
        _context.RecordAdminActivity(adminUserId, $"{action} tài khoản",
            $"Tài khoản #{user.Id} · {user.FullName}");
        await _context.SaveChangesAsync();

        return (true, $"{action} tài khoản \"{user.FullName}\" thành công.", user.IsActive);
    }

    public async Task<OwnerProfileDto?> GetOwnerProfileAsync(int ownerId)
        => await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == ownerId && user.Role == UserRole.CourtOwner)
            .Select(user => new OwnerProfileDto
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            })
            .SingleOrDefaultAsync();

    public async Task<(bool Success, string Message, OwnerProfileDto? Profile)> UpdateOwnerProfileAsync(
        int ownerId,
        OwnerProfileUpdateRequest request)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        if (fullName.Length is 0 or > 100
            || !Regex.IsMatch(phoneNumber, @"^(0|\+84)\d{9}$"))
        {
            return (false, "Thông tin hồ sơ không hợp lệ.", null);
        }

        var user = await _context.Users
            .SingleOrDefaultAsync(item => item.Id == ownerId && item.Role == UserRole.CourtOwner);
        if (user is null)
        {
            return (false, "Không tìm thấy tài khoản Chủ sân.", null);
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
        _context.RecordActivity(UserRole.CourtOwner, ownerId, "Cập nhật hồ sơ",
            $"Tài khoản #{ownerId}");
        await _context.SaveChangesAsync();

        return (true, "Đã cập nhật hồ sơ cá nhân.", new OwnerProfileDto
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<(bool Success, string Message)> ChangeOwnerPasswordAsync(
        int ownerId,
        OwnerChangePasswordRequest request)
    {
        var currentPassword = request.CurrentPassword ?? string.Empty;
        var newPassword = request.NewPassword ?? string.Empty;
        if (currentPassword.Length is 0 or > 100)
        {
            return (false, "Mật khẩu hiện tại không hợp lệ.");
        }

        if (newPassword.Length is < 8 or > 100)
        {
            return (false, "Mật khẩu mới phải từ 8 đến 100 ký tự.");
        }

        if (!string.Equals(newPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Mật khẩu xác nhận không khớp.");
        }

        var user = await _context.Users
            .SingleOrDefaultAsync(item => item.Id == ownerId && item.Role == UserRole.CourtOwner);
        if (user is null)
        {
            return (false, "Không tìm thấy tài khoản Chủ sân.");
        }

        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            return (false, "Mật khẩu hiện tại không chính xác.");
        }

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            return (false, "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _context.RecordActivity(UserRole.CourtOwner, ownerId, "Đổi mật khẩu",
            $"Tài khoản #{ownerId}");
        await _context.SaveChangesAsync();

        return (true, "Đổi mật khẩu thành công.");
    }
}
