using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

public class OwnerBookingsViewModel
{
    public List<CourtDto> Courts { get; set; } = new();
    public List<BookingDto> Bookings { get; set; } = new();
    public DateOnly? SelectedDate { get; set; }
    public int? SelectedCourtId { get; set; }
    public BookingStatus? SelectedStatus { get; set; }
}
