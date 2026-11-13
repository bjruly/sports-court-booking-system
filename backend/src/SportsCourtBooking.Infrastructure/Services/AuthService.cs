using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class AuthService : IAuthService
{
    // Hash giả dùng khi không tìm thấy email: vẫn chạy 1 lần Verify để thời gian phản hồi
    // không tiết lộ email nào đã đăng ký.
    private static readonly string DummyHash = string.Join(
        "$", "PBKDF2", "600000", Convert.ToBase64String(new byte[16]), Convert.ToBase64String(new byte[32]));

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<(bool Success, string Message, UserDto? User)> LoginAsync(LoginRequest request)
    {
        const string invalidMessage = "Email hoặc mật khẩu không chính xác.";

        var email = NormalizeEmail(request.Email);
        var password = request.Password ?? string.Empty;
        if (email.Length == 0 || password.Length == 0)
            return (false, invalidMessage, null);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user == null)
        {
            _passwordHasher.Verify(password, DummyHash);
            return (false, invalidMessage, null);
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            return (false, invalidMessage, null);

        return (true, "Đăng nhập thành công!", ToDto(user));
    }

    public async Task<(bool Success, string Message, UserDto? User)> RegisterAsync(RegisterRequest request)
    {
        var fullName = (request.FullName ?? string.Empty).Trim();
        var email = NormalizeEmail(request.Email);
        var phone = (request.PhoneNumber ?? string.Empty).Trim();
        var password = request.Password ?? string.Empty;

        if (fullName.Length == 0 || fullName.Length > 100)
            return (false, "Họ và tên phải từ 1 đến 100 ký tự.", null);
        if (email.Length == 0 || email.Length > 150 || !email.Contains('@'))
            return (false, "Email không hợp lệ.", null);
        if (phone.Length == 0 || phone.Length > 20)
            return (false, "Số điện thoại không hợp lệ.", null);
        if (password.Length < 8 || password.Length > 100)
            return (false, "Mật khẩu phải từ 8 đến 100 ký tự.", null);

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return (false, "Email này đã được sử dụng.", null);

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PhoneNumber = phone,
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.Customer,
            IsActive = true
        };

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Hai người đăng ký cùng email cùng lúc: chỉ mục unique của database sẽ từ chối người đến sau.
            _context.Entry(user).State = EntityState.Detached;
            return (false, "Email này đã được sử dụng.", null);
        }

        return (true, "Đăng ký tài khoản thành công!", ToDto(user));
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role
    };
}
