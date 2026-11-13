using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Entities;

namespace SportsCourtBooking.Web.Models;

public class BookingPageViewModel
{
    public List<CourtDto> Courts { get; set; } = new();
    public List<BookingDto> Bookings { get; set; } = new();
    public DateOnly SelectedDate { get; set; } = SportsCourtBooking.Application.Common.VietnamClock.Today;
    public int? SelectedCourtTypeId { get; set; }
    public int? SelectedCourtId { get; set; }
}