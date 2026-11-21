using System.ComponentModel.DataAnnotations;
using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class OwnerWalkInBookingViewModel
{
    public List<CourtDto> Courts { get; set; } = new();

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sân.")]
    public int CourtId { get; set; }

    public DateOnly BookingDate { get; set; }

    [Range(6, 21, ErrorMessage = "Giờ bắt đầu phải từ 06:00 đến 21:00.")]
    public int StartHour { get; set; } = 6;

    [Range(7, 22, ErrorMessage = "Giờ kết thúc phải từ 07:00 đến 22:00.")]
    public int EndHour { get; set; } = 7;

    [Required(ErrorMessage = "Vui lòng nhập tên khách.")]
    [StringLength(100, ErrorMessage = "Tên khách tối đa 100 ký tự.")]
    public string GuestName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại khách.")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ 0901234567).")]
    public string GuestPhone { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    public string? Note { get; set; }
}
