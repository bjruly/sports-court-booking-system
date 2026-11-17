using System.ComponentModel.DataAnnotations;

namespace SportsCourtBooking.Application.DTOs;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage = "Mật khẩu mới phải từ 8 đến 100 ký tự."
    )]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "Mật khẩu nhập lại không khớp."
    )]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}