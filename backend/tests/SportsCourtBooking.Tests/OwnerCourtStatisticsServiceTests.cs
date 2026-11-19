using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerCourtStatisticsServiceTests
{
    [Fact]
    public async Task GetMonthlyStatisticsAsync_IncludesOwnedCourtsAndCountsBookingsByStatus()
    {
        await using var context = CreateContext();
        var activeCourt = new Court { Id = 1, OwnerId = 7, Name = "Sân hoạt động", Status = CourtStatus.Active };
        var quietCourt = new Court { Id = 2, OwnerId = 7, Name = "Sân chưa có đơn", Status = CourtStatus.Maintenance };
        var otherOwnersCourt = new Court { Id = 3, OwnerId = 8, Name = "Sân chủ khác" };
        context.Courts.AddRange(activeCourt, quietCourt, otherOwnersCourt);

        context.Bookings.AddRange(
            CreateBooking(activeCourt, new DateOnly(2026, 11, 3), BookingStatus.Confirmed),
            CreateBooking(activeCourt, new DateOnly(2026, 11, 4), BookingStatus.Completed),
            CreateBooking(activeCourt, new DateOnly(2026, 11, 5), BookingStatus.Pending),
            CreateBooking(activeCourt, new DateOnly(2026, 11, 6), BookingStatus.Cancelled),
            CreateBooking(activeCourt, new DateOnly(2026, 11, 7), BookingStatus.Expired),
            CreateBooking(activeCourt, new DateOnly(2026, 12, 1), BookingStatus.Confirmed),
            CreateBooking(otherOwnersCourt, new DateOnly(2026, 11, 8), BookingStatus.Confirmed));
        await context.SaveChangesAsync();

        var statistics = await new OwnerCourtStatisticsService(context)
            .GetMonthlyStatisticsAsync(ownerId: 7, year: 2026, month: 11);

        Assert.Equal(new DateOnly(2026, 11, 1), statistics.MonthStart);
        Assert.Equal(2, statistics.TotalCourts);
        Assert.Equal(1, statistics.ActiveCourts);
        Assert.Equal(5, statistics.TotalBookings);
        Assert.Equal(2, statistics.SuccessfulBookings);
        Assert.Equal(1, statistics.PendingBookings);
        Assert.Equal(1, statistics.CancelledBookings);
        Assert.Equal(1, statistics.ExpiredBookings);

        var activeCourtStatistics = Assert.Single(statistics.Courts, court => court.CourtId == activeCourt.Id);
        Assert.Equal(5, activeCourtStatistics.TotalBookings);
        Assert.Equal(2, activeCourtStatistics.SuccessfulBookings);
        Assert.Equal(40d, activeCourtStatistics.SuccessRatePercent);

        var quietCourtStatistics = Assert.Single(statistics.Courts, court => court.CourtId == quietCourt.Id);
        Assert.Equal(0, quietCourtStatistics.TotalBookings);
        Assert.Equal(0d, quietCourtStatistics.SuccessRatePercent);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2026, 13)]
    public async Task GetMonthlyStatisticsAsync_RejectsInvalidPeriod(int year, int month)
    {
        await using var context = CreateContext();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new OwnerCourtStatisticsService(context).GetMonthlyStatisticsAsync(7, year, month));
    }

    private static Booking CreateBooking(Court court, DateOnly bookingDate, BookingStatus status)
    {
        return new Booking
        {
            Court = court,
            CourtId = court.Id,
            BookingDate = bookingDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = status
        };
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
