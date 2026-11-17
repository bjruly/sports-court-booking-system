using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerWalkInBookingTests
{
    [Fact]
    public async Task CreateOwnerWalkInBookingAsync_CreatesConfirmedBookingForGuest()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);
        var request = CreateRequest();

        var result = await service.CreateOwnerWalkInBookingAsync(3, request);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("Khách đặt tại quầy", result.Data.CustomerName);
        Assert.Equal("0901234567", result.Data.CustomerPhone);

        var booking = await context.Bookings.SingleAsync();
        Assert.Null(booking.UserId);
        Assert.Equal("Khách đặt tại quầy", booking.GuestName);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Null(booking.ExpiresAt);
    }

    [Fact]
    public async Task CreateOwnerWalkInBookingAsync_RejectsCourtOwnedByAnotherOwner()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);

        var result = await service.CreateOwnerWalkInBookingAsync(999, CreateRequest());

        Assert.False(result.Success);
        Assert.Empty(await context.Bookings.ToListAsync());
    }

    [Fact]
    public async Task CreateOwnerWalkInBookingAsync_RejectsMissingCustomerContact()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);
        var request = CreateRequest();
        request.GuestPhone = "not-a-phone";

        var result = await service.CreateOwnerWalkInBookingAsync(3, request);

        Assert.False(result.Success);
        Assert.Empty(await context.Bookings.ToListAsync());
    }

    [Fact]
    public async Task CreateOwnerWalkInBookingAsync_RejectsOverlappingBooking()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);

        var first = await service.CreateOwnerWalkInBookingAsync(3, CreateRequest());
        var overlappingRequest = CreateRequest();
        overlappingRequest.StartTime = new TimeOnly(11, 0);
        overlappingRequest.EndTime = new TimeOnly(12, 0);
        var overlapping = await service.CreateOwnerWalkInBookingAsync(3, overlappingRequest);

        Assert.True(first.Success, first.Message);
        Assert.False(overlapping.Success);
        Assert.Single(await context.Bookings.ToListAsync());
    }

    [Fact]
    public async Task GetOwnerBookingsForCalendarAsync_IncludesGuestContactAndOnlyOwnedCourts()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);
        var request = CreateRequest();
        await service.CreateOwnerWalkInBookingAsync(3, request);
        context.Users.Add(new User
        {
            Id = 4,
            FullName = "Chủ sân khác",
            Email = "owner4@example.com",
            PhoneNumber = "0900000004",
            PasswordHash = "test-hash",
            Role = UserRole.CourtOwner,
            IsActive = true
        });
        context.Courts.Add(new Court
        {
            Id = 5,
            OwnerId = 4,
            Name = "Sân của chủ khác",
            CourtTypeId = 1,
            Location = "Khu khác",
            PricePerHour = 100_000,
            Status = CourtStatus.Active
        });
        context.Bookings.Add(new Booking
        {
            CourtId = 5,
            GuestName = "Khách của chủ khác",
            GuestPhone = "0900000005",
            BookingDate = request.BookingDate,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0),
            TotalPrice = 100_000,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var bookings = await service.GetOwnerBookingsForCalendarAsync(
            3,
            request.BookingDate.AddDays(-1),
            request.BookingDate.AddDays(1));

        var booking = Assert.Single(bookings);
        Assert.Equal("Khách đặt tại quầy", booking.CustomerName);
        Assert.Equal("0901234567", booking.CustomerPhone);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_KeepsCustomerBookingPendingWithExpiration()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);

        var result = await service.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = 2,
            CourtId = 1,
            BookingDate = VietnamClock.Today.AddDays(1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0)
        });

        Assert.True(result.Success, result.Message);
        var booking = await context.Bookings.SingleAsync();
        Assert.Equal(2, booking.UserId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.NotNull(booking.ExpiresAt);
    }

    private static BookingService CreateService(AppDbContext context)
        => new(
            context,
            NullLogger<BookingService>.Instance,
            new PendingBookingExpirationService(context));

    private static OwnerWalkInBookingRequest CreateRequest() => new()
    {
        CourtId = 1,
        BookingDate = VietnamClock.Today.AddDays(1),
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(12, 0),
        GuestName = "  Khách đặt tại quầy  ",
        GuestPhone = " 0901234567 "
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }
}
