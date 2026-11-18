using System.ComponentModel.DataAnnotations;

namespace SportsCourtBooking.Application.DTOs;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(
        100,
        ErrorMessage = "Họ và tên tối đa 100 ký tự."
    )]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(
        20,
        ErrorMessage = "Số điện thoại tối đa 20 ký tự."
    )]
    [RegularExpression(
        @"^(0|\+84)\d{9}$",
        ErrorMessage = "Số điện thoại không hợp lệ, ví dụ 0901234567."
    )]
    public string PhoneNumber { get; set; } = string.Empty;
}