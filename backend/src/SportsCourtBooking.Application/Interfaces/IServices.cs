using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.Interfaces;

public interface ICourtService
{
    /// <param name="onlyActive">true: chỉ lấy sân đang hoạt động (dùng cho trang khách hàng).</param>
    Task<List<CourtDto>> GetAllCourtsAsync(int? courtTypeId = null, bool onlyActive = false);
    Task<CourtDto?> GetCourtByIdAsync(int id);
}

public interface IBookingService
{
    /// <param name="includeInactive">false: chỉ lấy đơn đang giữ giờ (Pending/Confirmed/Completed), dùng cho lưới lịch của khách.
    /// true: lấy cả đơn đã hủy/hết hạn, dùng cho trang quản trị.</param>
    Task<List<BookingDto>> GetBookingsByDateAsync(DateOnly date, int? courtId = null, bool includeInactive = false);
    Task<List<BookingDto>> GetBookingsByUserAsync(int userId);
    Task<BookingDetailDto?> GetOwnBookingAsync(int bookingId, int userId);
    Task<bool> IsTimeSlotAvailableAsync(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
    Task<(bool Success, string Message, BookingDto? Data)> CreateBookingAsync(CreateBookingRequest request);

    /// <summary>Admin/Staff xác nhận đơn Pending còn hạn giữ chỗ.</summary>
    Task<(bool Success, string Message)> ConfirmBookingAsync(int bookingId, int actorUserId);

    /// <summary>Admin/Staff hủy đơn Pending hoặc Confirmed; khung giờ được nhả ngay.</summary>
    Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, int actorUserId);
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

public interface IUserService
{
    /// <summary>Lấy danh sách khách hàng (Role = Customer), hỗ trợ tìm kiếm theo tên/email/SĐT.</summary>
    Task<List<UserDto>> GetCustomersAsync(string? search = null);

    /// <summary>Khóa hoặc mở khóa tài khoản. Trả về (Success, Message, trạng thái IsActive mới).</summary>
    Task<(bool Success, string Message, bool? IsActive)> ToggleLockAsync(int targetUserId, int? adminUserId);
}

public interface IReportService
{
    /// <summary>Lấy số liệu báo cáo tổng hợp toàn hệ thống cho Admin.</summary>
    Task<AdminReportDto> GetAdminReportAsync();
}

public interface IOwnerDashboardService
{
    Task<OwnerDashboardDto> GetDashboardAsync(int ownerId);
}

public interface IOwnerCourtService
{
    Task<List<CourtDto>> GetOwnerCourtsAsync(int ownerId);
    Task<List<CourtTypeDto>> GetCourtTypesAsync();
    Task<CourtDto?> GetOwnerCourtAsync(int courtId, int ownerId);
    Task<(bool Success, string Message)> CreateOwnerCourtAsync(int ownerId, OwnerCourtRequest request);
    Task<(bool Success, string Message)> UpdateOwnerCourtAsync(int courtId, int ownerId, OwnerCourtRequest request);
    Task<(bool Success, string Message)> UpdateOwnerCourtStatusAsync(int courtId, int ownerId, CourtStatus status);
}
