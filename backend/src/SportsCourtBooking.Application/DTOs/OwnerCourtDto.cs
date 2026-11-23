using System.ComponentModel.DataAnnotations;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class OwnerCourtRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên sân.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên sân phải từ 2 đến 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn cơ sở.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn cơ sở hợp lệ.")]
    public int? FacilityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn thể thao.")]
    public int CourtTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa điểm.")]
    [StringLength(255, ErrorMessage = "Địa điểm không được vượt quá 255 ký tự.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập quận/huyện.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Quận/huyện phải từ 2 đến 100 ký tự.")]
    public string District { get; set; } = "Quận 10";

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 500 ký tự.")]
    [Url(ErrorMessage = "Đường dẫn hình ảnh không hợp lệ.")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá mỗi giờ.")]
    [Range(typeof(decimal), "1", "9999999999999999", ErrorMessage = "Giá mỗi giờ phải lớn hơn 0.")]
    public decimal? PricePerHour { get; set; }

    [Range(BookingRules.OpeningHour, BookingRules.ClosingHour - 1, ErrorMessage = "Giờ bắt đầu cao điểm phải từ 06:00 đến 21:00.")]
    public int PeakStartHour { get; set; } = BookingRules.PeakStartHour;

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Phụ thu cao điểm không được âm.")]
    public decimal PeakSurcharge { get; set; } = BookingRules.PeakSurcharge;

    [EnumDataType(typeof(CourtStatus), ErrorMessage = "Trạng thái sân không hợp lệ.")]
    public CourtStatus Status { get; set; } = CourtStatus.Active;
}

public class CourtTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class OwnerFacilityRequest
{
    private string _phoneNumber = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên cơ sở.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên cơ sở phải từ 2 đến 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ cơ sở.")]
    [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
    public string Address { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Quận/Huyện không được vượt quá 100 ký tự.")]
    public string District { get; set; } = "Quận 10";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam (10 số).")]
    public string PhoneNumber
    {
        get => _phoneNumber;
        // Số thường được viết cách cho dễ đọc ("0901 111 001"); bỏ khoảng trắng trước khi kiểm tra và lưu.
        set => _phoneNumber = string.Concat((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character)));
    }

    [Required(ErrorMessage = "Vui lòng chọn môn thể thao duy nhất cho cơ sở.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn thể thao.")]
    public int CourtTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng sân đăng ký.")]
    [Range(1, 30, ErrorMessage = "Số lượng sân đăng ký từ 1 đến 30 sân.")]
    public int NumberOfCourts { get; set; } = 3;

    [Required(ErrorMessage = "Vui lòng nhập giá thuê mỗi giờ.")]
    [Range(10000, 50000000, ErrorMessage = "Giá thuê từ 10.000 đ đến 50.000.000 đ/giờ.")]
    public decimal PricePerHour { get; set; } = 80000;

    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "22:00";

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }
}

public class OwnerFacilityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    /// <summary>Quận/huyện của sân đầu tiên trong cơ sở; null nếu cơ sở chưa có sân.</summary>
    public string? District { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "22:00";
    public string? Description { get; set; }
    public int? CourtTypeId { get; set; }
    public string? CourtTypeName { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public int CourtCount { get; set; }
    public int ActiveCourtCount { get; set; }
    public int MaintenanceCourtCount { get; set; }
    public int InactiveCourtCount => Math.Max(0, CourtCount - ActiveCourtCount - MaintenanceCourtCount);
}
