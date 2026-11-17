using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class CourtDetailsViewModel
{
    public CourtDto Court { get; set; } = new();

    public DateOnly PlayDate { get; set; }
}