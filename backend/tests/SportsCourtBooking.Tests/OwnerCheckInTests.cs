using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerCheckInTests
{
    [Fact]
    public async Task CheckInOwnerBookingAsync_RecordsCheckInWithoutChangingStatus_AndRejectsDuplicate()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var booking = await AddBookingAsync(context, BookingStatus.Confirmed, VietnamClock.Today);
        var service = CreateService(context);

        var result = await service.CheckInOwnerBookingAsync(booking.Id, 3, 3);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(booking.CheckInAt);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        var duplicate = await service.CheckInOwnerBookingAsync(booking.Id, 3, 3);
        Assert.False(duplicate.Success);
        Assert.Contains("đã được check-in", duplicate.Message);
    }

    [Fact]
    public async Task CheckInOwnerBookingAsync_RejectsBookingOutsideOwnerScope()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var booking = await AddBookingAsync(context, BookingStatus.Confirmed, VietnamClock.Today);
        var service = CreateService(context);

        var result = await service.CheckInOwnerBookingAsync(booking.Id, 999, 999);

        Assert.False(result.Success);
        Assert.Null(booking.CheckInAt);
    }

    [Theory]
    [InlineData(BookingStatus.Pending, 0)]
    [InlineData(BookingStatus.Confirmed, 1)]
    public async Task CheckInOwnerBookingAsync_RejectsWrongStatusOrDate(BookingStatus status, int dayOffset)
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var booking = await AddBookingAsync(context, status, VietnamClock.Today.AddDays(dayOffset));
        var service = CreateService(context);

        var result = await service.CheckInOwnerBookingAsync(booking.Id, 3, 3);

        Assert.False(result.Success);
        Assert.Null(booking.CheckInAt);
    }

    private static async Task<Booking> AddBookingAsync(
        AppDbContext context,
        BookingStatus status,
        DateOnly bookingDate)
    {
        var booking = new Booking
        {
            UserId = 2,
            CourtId = 1,
            BookingDate = bookingDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            TotalPrice = 80_000,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return booking;
    }

    private static BookingService CreateService(AppDbContext context)
        => new(
            context,
            NullLogger<BookingService>.Instance,
            new PendingBookingExpirationService(context));

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }
}
