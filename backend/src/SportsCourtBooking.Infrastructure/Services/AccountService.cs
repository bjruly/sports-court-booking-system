using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AccountService(
    AppDbContext context,
    IPasswordHasher passwordHasher)
    {
    _context = context;
    _passwordHasher = passwordHasher;
    }

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
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
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request)
    {
        if (request is null)
        {
            return (false, "Không nhận được thông tin cập nhật.");
        }

        var input = new UpdateProfileRequest
        {
            FullName = (request.FullName ?? string.Empty).Trim(),
            PhoneNumber = (request.PhoneNumber ?? string.Empty).Trim()
        };

        var errors = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            input,
            new ValidationContext(input),
            errors,
            validateAllProperties: true
        );

        if (!isValid)
        {
            var message = errors.FirstOrDefault()?.ErrorMessage
                ?? "Thông tin cập nhật không hợp lệ.";

            return (false, message);
        }

        var nowUtc = DateTime.UtcNow;

        var updated = await _context.Users
            .Where(u => u.Id == userId && u.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.FullName, input.FullName)
                .SetProperty(u => u.PhoneNumber, input.PhoneNumber)
                .SetProperty(u => u.UpdatedAt, nowUtc)
            );

        if (updated == 0)
        {
            return (
                false,
                "Tài khoản không tồn tại hoặc đã bị khóa."
            );
        }

        return (true, "Cập nhật hồ sơ thành công.");

        
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(
    int userId,
    ChangePasswordRequest request)
{
    if (request is null)
    {
        return (false, "Không nhận được thông tin đổi mật khẩu.");
    }

    var errors = new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(
        request,
        new ValidationContext(request),
        errors,
        validateAllProperties: true
    );

    if (!isValid)
    {
        var message = errors.FirstOrDefault()?.ErrorMessage
            ?? "Thông tin đổi mật khẩu không hợp lệ.";

        return (false, message);
    }

    var user = await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

    if (user is null)
    {
        return (
            false,
            "Tài khoản không tồn tại hoặc đã bị khóa."
        );
    }

    var correctPassword = _passwordHasher.Verify(
        request.CurrentPassword,
        user.PasswordHash
    );

    if (!correctPassword)
    {
        return (false, "Mật khẩu hiện tại không chính xác.");
    }

    if (request.NewPassword == request.CurrentPassword)
    {
        return (
            false,
            "Mật khẩu mới phải khác mật khẩu hiện tại."
        );
    }

    var newPasswordHash = _passwordHasher.Hash(
        request.NewPassword
    );

    var nowUtc = DateTime.UtcNow;

    var updated = await _context.Users
        .Where(u =>
            u.Id == userId
            && u.IsActive
            && u.PasswordHash == user.PasswordHash
        )
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(u => u.PasswordHash, newPasswordHash)
            .SetProperty(u => u.UpdatedAt, nowUtc)
        );

    if (updated == 0)
    {
        return (
            false,
            "Thông tin tài khoản đã thay đổi. Vui lòng đăng nhập lại và thử lại."
        );
    }

    return (
        true,
        "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
    );
}
}