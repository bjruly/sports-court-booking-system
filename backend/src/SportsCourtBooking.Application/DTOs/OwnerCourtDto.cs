using System.ComponentModel.DataAnnotations;
using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class OwnerCourtRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên sân.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên sân phải từ 2 đến 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn thể thao.")]
    public int CourtTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa điểm.")]
    [StringLength(255, ErrorMessage = "Địa điểm không được vượt quá 255 ký tự.")]
    public string Location { get; set; } = string.Empty;

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
