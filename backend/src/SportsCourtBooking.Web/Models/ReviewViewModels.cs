using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Web.Models;

public class CustomerReviewFormViewModel : CustomerReviewRequest
{
    public int BookingId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsEditing { get; set; }
}

public class OwnerReviewsViewModel
{
    public List<CourtDto> Courts { get; set; } = new();
    public List<OwnerReviewDto> Reviews { get; set; } = new();
    public int? SelectedCourtId { get; set; }
}
