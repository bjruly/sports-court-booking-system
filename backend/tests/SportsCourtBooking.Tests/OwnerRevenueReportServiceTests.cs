using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerRevenueReportServiceTests
{
    [Fact]
    public async Task GetMonthlyReportAsync_SeparatesBookingValuePaymentsAndPaidCancelledBookings()
    {
        await using var context = CreateContext();
        var ownerCourt = new Court { Id = 1, OwnerId = 7, Name = "Sân chủ sở hữu" };
        var otherOwnersCourt = new Court { Id = 2, OwnerId = 8, Name = "Sân khác" };
        context.Courts.AddRange(ownerCourt, otherOwnersCourt);

        context.Bookings.AddRange(
            CreateBooking(ownerCourt, new DateOnly(2026, 11, 3), BookingStatus.Confirmed, 100m,
                PaymentStatus.Paid, 100m),
            CreateBooking(ownerCourt, new DateOnly(2026, 11, 4), BookingStatus.Completed, 120m,
                PaymentStatus.Refunded, 120m),
            CreateBooking(ownerCourt, new DateOnly(2026, 11, 5), BookingStatus.Cancelled, 80m,
                PaymentStatus.Paid, 80m),
            CreateBooking(ownerCourt, new DateOnly(2026, 11, 6), BookingStatus.Expired, 50m,
                PaymentStatus.Paid, 50m),
            CreateBooking(ownerCourt, new DateOnly(2026, 11, 7), BookingStatus.Pending, 25m,
                PaymentStatus.Unpaid, 25m),
            CreateBooking(ownerCourt, new DateOnly(2026, 12, 1), BookingStatus.Confirmed, 200m,
                PaymentStatus.Paid, 200m),
            CreateBooking(otherOwnersCourt, new DateOnly(2026, 11, 8), BookingStatus.Confirmed, 300m,
                PaymentStatus.Paid, 300m));
        await context.SaveChangesAsync();

        var report = await new OwnerRevenueReportService(context)
            .GetMonthlyReportAsync(ownerId: 7, year: 2026, month: 11);

        Assert.Equal(5, report.TotalBookings);
        Assert.Equal(1, report.ConfirmedBookings);
        Assert.Equal(1, report.CompletedBookings);
        Assert.Equal(1, report.PendingBookings);
        Assert.Equal(1, report.CancelledBookings);
        Assert.Equal(1, report.ExpiredBookings);
        Assert.Equal(220m, report.BookingValue);
        Assert.Equal(230m, report.CollectedAmount);
        Assert.Equal(120m, report.RefundedAmount);
        Assert.Equal(130m, report.CollectedForCancelledBookings);
        Assert.Single(report.CourtReports);
        Assert.Equal(30, report.DailyReports.Count);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_CanFilterToAnOwnedCourt()
    {
        await using var context = CreateContext();
        var firstCourt = new Court { Id = 1, OwnerId = 7, Name = "Sân một" };
        var secondCourt = new Court { Id = 2, OwnerId = 7, Name = "Sân hai" };
        context.Courts.AddRange(firstCourt, secondCourt);
        context.Bookings.AddRange(
            CreateBooking(firstCourt, new DateOnly(2026, 11, 3), BookingStatus.Confirmed, 100m),
            CreateBooking(secondCourt, new DateOnly(2026, 11, 4), BookingStatus.Confirmed, 200m));
        await context.SaveChangesAsync();

        var report = await new OwnerRevenueReportService(context)
            .GetMonthlyReportAsync(ownerId: 7, year: 2026, month: 11, courtId: 2);

        Assert.Equal(200m, report.BookingValue);
        Assert.Single(report.CourtReports);
        Assert.Equal(2, report.CourtReports[0].CourtId);
    }

    private static Booking CreateBooking(
        Court court,
        DateOnly bookingDate,
        BookingStatus status,
        decimal totalPrice,
        PaymentStatus? paymentStatus = null,
        decimal? paymentAmount = null)
    {
        var booking = new Booking
        {
            Court = court,
            CourtId = court.Id,
            BookingDate = bookingDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = status,
            TotalPrice = totalPrice
        };

        if (paymentStatus.HasValue)
        {
            booking.Payment = new Payment
            {
                Booking = booking,
                Amount = paymentAmount ?? totalPrice,
                Status = paymentStatus.Value
            };
        }

        return booking;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
