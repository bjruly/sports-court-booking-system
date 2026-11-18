using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

public class BookingHistoryViewModel
{
    public List<BookingDto> Bookings { get; set; } = new();

    public BookingStatus? Status { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }
}