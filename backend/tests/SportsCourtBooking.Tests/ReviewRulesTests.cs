using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Tests;

public class ReviewRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0);

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Expired)]
    public void CanReview_RejectsBookingsNotConfirmed(BookingStatus status)
    {
        Assert.False(ReviewRules.CanReview(
            status, new DateOnly(2026, 11, 11), new TimeOnly(9, 0), Now));
    }

    [Fact]
    public void CanReview_AcceptsConfirmedBookingAfterEndTime()
    {
        Assert.True(ReviewRules.CanReview(
            BookingStatus.Confirmed, new DateOnly(2026, 11, 12), new TimeOnly(10, 0), Now));
    }

    [Fact]
    public void CanReview_RejectsBookingBeforeEndTime()
    {
        Assert.False(ReviewRules.CanReview(
            BookingStatus.Confirmed, new DateOnly(2026, 11, 12), new TimeOnly(11, 0), Now));
    }

    [Fact]
    public void CanReview_AcceptsConfirmedBookingOnEarlierDate()
    {
        Assert.True(ReviewRules.CanReview(
            BookingStatus.Confirmed, new DateOnly(2026, 11, 11), new TimeOnly(22, 0), Now));
    }
}
