using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class OwnerProfileViewModel : OwnerProfileUpdateRequest
{
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
