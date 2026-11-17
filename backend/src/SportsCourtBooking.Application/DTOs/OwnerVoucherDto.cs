using System.ComponentModel.DataAnnotations;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.DTOs;

public class OwnerVoucherRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã voucher.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Mã voucher phải từ 3 đến 30 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]{2,29}$", ErrorMessage = "Mã chỉ gồm chữ, số, dấu gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [EnumDataType(typeof(VoucherDiscountType), ErrorMessage = "Loại giảm giá không hợp lệ.")]
    public VoucherDiscountType DiscountType { get; set; } = VoucherDiscountType.Percentage;

    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    public decimal DiscountValue { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    public DateOnly? EndDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Giới hạn lượt dùng phải lớn hơn 0.")]
    public int? UsageLimit { get; set; }

    public bool IsActive { get; set; } = true;
}

public class OwnerVoucherDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public VoucherDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
}

public class VoucherApplicationDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
}
