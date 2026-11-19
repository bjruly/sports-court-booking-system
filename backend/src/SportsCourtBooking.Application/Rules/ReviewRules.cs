using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.Rules;

public static class ReviewRules
{
    public static bool CanReview(
        BookingStatus status,
        DateOnly bookingDate,
        TimeOnly endTime,
        DateTime nowVietnam)
    {
        if (status != BookingStatus.Confirmed)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(nowVietnam);
        var currentTime = TimeOnly.FromDateTime(nowVietnam);
        return bookingDate < today || (bookingDate == today && endTime <= currentTime);
    }
}
