using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Application.Interfaces;

public interface ICourtService
{
    /// <param name="onlyActive">true: chỉ lấy sân đang hoạt động (dùng cho trang khách hàng).</param>
    Task<List<CourtDto>> GetAllCourtsAsync(int? courtTypeId = null, bool onlyActive = false);
    Task<CourtDto?> GetCourtByIdAsync(int id);
}

public interface IBookingService
{
    Task<List<BookingDto>> GetBookingsByDateAsync(DateOnly date, int? courtId = null);
    Task<List<BookingDto>> GetBookingsByUserAsync(int userId);
    Task<bool> IsTimeSlotAvailableAsync(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
    Task<(bool Success, string Message, BookingDto? Data)> CreateBookingAsync(CreateBookingRequest request);
}

public interface IAuthService
{
    Task<(bool Success, string Message, UserDto? User)> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message, UserDto? User)> RegisterAsync(RegisterRequest request);
}

/// <summary>Băm và kiểm tra mật khẩu. Không bao giờ lưu mật khẩu dạng chữ thường.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
