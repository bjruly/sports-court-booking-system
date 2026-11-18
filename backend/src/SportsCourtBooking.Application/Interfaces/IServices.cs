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
    Task<List<BookingDto>> GetOwnerBookingsAsync(int ownerId, DateOnly? date = null, int? courtId = null, BookingStatus? status = null);
    Task<List<BookingDto>> GetOwnerBookingsForCalendarAsync(int ownerId, DateOnly startDate, DateOnly endDate, int? courtId = null);
    Task<List<BookingDto>> GetBookingsByUserAsync(
        int userId,
        BookingStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);
    Task<BookingDetailDto?> GetOwnBookingAsync(int bookingId, int userId);
    Task<(bool Success, string Message)> CancelOwnBookingAsync(
    int bookingId,
    int userId);
    Task<bool> IsTimeSlotAvailableAsync(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
    Task<(bool Success, string Message, BookingDto? Data)> CreateBookingAsync(CreateBookingRequest request);
    Task<(bool Success, string Message, BookingDto? Data)> CreateOwnerWalkInBookingAsync(int ownerId, OwnerWalkInBookingRequest request);

    /// <summary>Admin/Staff xác nhận đơn Pending còn hạn giữ chỗ.</summary>
    Task<(bool Success, string Message)> ConfirmBookingAsync(int bookingId, int actorUserId);
    Task<(bool Success, string Message)> ConfirmOwnerBookingAsync(int bookingId, int ownerId, int actorUserId);
    Task<(bool Success, string Message)> CheckInOwnerBookingAsync(int bookingId, int ownerId, int actorUserId);

    /// <summary>Admin/Staff hủy đơn Pending hoặc Confirmed; khung giờ được nhả ngay.</summary>
    Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, int actorUserId);
    Task<(bool Success, string Message)> CancelOwnerBookingAsync(int bookingId, int ownerId, int actorUserId);
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
    Task<OwnerProfileDto?> GetOwnerProfileAsync(int ownerId);
    Task<(bool Success, string Message, OwnerProfileDto? Profile)> UpdateOwnerProfileAsync(
        int ownerId,
        OwnerProfileUpdateRequest request);
    Task<(bool Success, string Message)> ChangeOwnerPasswordAsync(int ownerId, OwnerChangePasswordRequest request);
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

public interface IOwnerCourtStatisticsService
{
    Task<OwnerCourtStatisticsDto> GetMonthlyStatisticsAsync(int ownerId, int year, int month);
}

public interface IOwnerRevenueReportService
{
    Task<OwnerRevenueReportDto> GetMonthlyReportAsync(
        int ownerId,
        int year,
        int month,
        int? courtId = null);
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

public interface IOwnerFacilityService
{
    Task<List<OwnerFacilityDto>> GetOwnerFacilitiesAsync(int ownerId);
    Task<OwnerFacilityDto?> GetOwnerFacilityAsync(int facilityId, int ownerId);
    Task<(bool Success, string Message)> CreateOwnerFacilityAsync(int ownerId, OwnerFacilityRequest request);
    Task<(bool Success, string Message)> UpdateOwnerFacilityAsync(
        int facilityId,
        int ownerId,
        OwnerFacilityRequest request);
    Task<(bool Success, string Message)> DeleteOwnerFacilityAsync(int facilityId, int ownerId);
}

public interface IVoucherService
{
    Task<List<OwnerVoucherDto>> GetOwnerVouchersAsync(int ownerId);
    Task<OwnerVoucherDto?> GetOwnerVoucherAsync(int voucherId, int ownerId);
    Task<(bool Success, string Message)> CreateOwnerVoucherAsync(int ownerId, OwnerVoucherRequest request);
    Task<(bool Success, string Message)> UpdateOwnerVoucherAsync(int voucherId, int ownerId, OwnerVoucherRequest request);
    Task<(bool Success, string Message, bool? IsActive)> ToggleOwnerVoucherAsync(int voucherId, int ownerId);
    Task<VoucherApplicationDto> ValidateForCourtAsync(int courtId, DateOnly applicationDate, decimal subtotal, string code);
}

public interface IReviewService
{
    Task<List<OwnerReviewDto>> GetOwnerReviewsAsync(int ownerId, int? courtId = null);
    Task<CustomerReviewFormDto?> GetCustomerReviewFormAsync(int bookingId, int userId);
    Task<(bool Success, string Message)> SaveCustomerReviewAsync(int bookingId, int userId, CustomerReviewRequest request);
    Task<(bool Success, string Message)> ReplyToReviewAsync(int reviewId, int ownerId, string reply);
}
