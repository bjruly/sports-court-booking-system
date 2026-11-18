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
}

