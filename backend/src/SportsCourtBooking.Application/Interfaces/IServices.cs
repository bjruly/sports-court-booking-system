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
    Task<(bool Success, string Message)> RefundOwnerBookingAsync(int bookingId, int ownerId, int actorUserId);
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

    /// <summary>Lấy danh sách chủ sân (Role = CourtOwner) kèm số cơ sở, số sân và hỗ trợ tìm kiếm/lọc.</summary>
    Task<List<CourtOwnerItemDto>> GetCourtOwnersAsync(string? search = null, string? filter = null);

    /// <summary>Lấy thống kê số lượng chủ sân theo trạng thái.</summary>
    Task<CourtOwnerStatsDto> GetCourtOwnerStatsAsync();

    /// <summary>Khóa hoặc mở khóa tài khoản chủ sân.</summary>
    Task<(bool Success, string Message, bool? IsActive)> ToggleLockOwnerAsync(int targetOwnerId, int? adminUserId);

    /// <summary>Kiểm tra tài khoản người dùng có đang hoạt động không.</summary>
    Task<bool> IsUserActiveAsync(int userId);

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

    /// <summary>Lấy báo cáo chi tiết theo danh mục môn thể thao / loại sân.</summary>
    Task<List<CategoryDetailReportDto>> GetCategoryReportsAsync();
}

public interface ICategoryService
{
    /// <summary>Lấy danh sách tất cả các danh mục môn thể thao / loại sân.</summary>
    Task<List<CategoryDto>> GetAllCategoriesAsync(string? search = null);

    /// <summary>Lấy thông tin chi tiết một danh mục theo Id.</summary>
    Task<CategoryDto?> GetCategoryByIdAsync(int id);

    /// <summary>Thêm mới danh mục.</summary>
    Task<(bool Success, string Message, CategoryDto? Data)> CreateCategoryAsync(CreateCategoryRequest request);

    /// <summary>Cập nhật danh mục.</summary>
    Task<(bool Success, string Message)> UpdateCategoryAsync(UpdateCategoryRequest request);

    /// <summary>Xóa danh mục (chỉ xóa khi chưa có sân nào thuộc danh mục).</summary>
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
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
    Task<(bool Success, string Message)> AddCourtToFacilityAsync(int facilityId, int ownerId);
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
    /// <summary>UC26: Lấy danh sách đánh giá phục vụ kiểm duyệt, hỗ trợ tìm kiếm và lọc vi phạm/đã ẩn.</summary>
    Task<List<ReviewItemDto>> GetAllReviewsAsync(string? search = null, string? filter = null);

    /// <summary>Thống kê số lượng đánh giá theo từng trạng thái.</summary>
    Task<ReviewStatsSummaryDto> GetReviewStatsAsync();

    /// <summary>UC26: Ẩn đánh giá vi phạm kèm lý do.</summary>
    Task<(bool Success, string Message)> HideReviewAsync(int reviewId, string reason);

    /// <summary>UC26: Bỏ ẩn / khôi phục hiển thị đánh giá.</summary>
    Task<(bool Success, string Message)> UnhideReviewAsync(int reviewId);

    /// <summary>UC26: Xóa vĩnh viễn đánh giá vi phạm.</summary>
    Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId);

    Task<List<OwnerReviewDto>> GetOwnerReviewsAsync(int ownerId, int? courtId = null);
    Task<CustomerReviewFormDto?> GetCustomerReviewFormAsync(int bookingId, int userId);
    Task<(bool Success, string Message)> SaveCustomerReviewAsync(int bookingId, int userId, CustomerReviewRequest request);
    Task<(bool Success, string Message)> ReplyToReviewAsync(int reviewId, int ownerId, string reply);
}

public interface IFacilityService
{
    /// <summary>Lấy danh sách cơ sở thể thao kèm bộ lọc tìm kiếm và trạng thái (UC13, UC23).</summary>
    Task<List<FacilityItemDto>> GetAllFacilitiesAsync(string? search = null, string? filter = null);

    /// <summary>Thống kê số lượng cơ sở theo các trạng thái.</summary>
    Task<FacilityStatsDto> GetFacilityStatsAsync();

    /// <summary>Lấy chi tiết một cơ sở theo Id.</summary>
    Task<FacilityItemDto?> GetFacilityByIdAsync(int id);

    /// <summary>Thêm mới cơ sở thể thao (UC13).</summary>
    Task<(bool Success, string Message, FacilityItemDto? Data)> CreateFacilityAsync(CreateFacilityRequest request);

    /// <summary>Cập nhật thông tin cơ sở thể thao (UC13).</summary>
    Task<(bool Success, string Message)> UpdateFacilityAsync(UpdateFacilityRequest request);

    /// <summary>Duyệt cơ sở thể thao (UC23).</summary>
    Task<(bool Success, string Message)> ApproveFacilityAsync(int facilityId);

    /// <summary>Khóa hoặc mở khóa cơ sở thể thao kèm lý do (UC23).</summary>
    Task<(bool Success, string Message, bool? IsActive)> ToggleLockFacilityAsync(int facilityId, string? reason);
}
