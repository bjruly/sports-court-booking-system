using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
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
        await _context.SaveChangesAsync();

        var action = user.IsActive ? "Mở khóa" : "Khóa";
        return (true, $"{action} tài khoản \"{user.FullName}\" thành công.", user.IsActive);
    }
}
