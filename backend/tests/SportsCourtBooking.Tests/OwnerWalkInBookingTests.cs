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
        Assert.NotNull(booking.Payment);
        Assert.Equal(PaymentStatus.Paid, booking.Payment.Status);
        Assert.Equal(PaymentMethodType.CashAtVenue.ToString(), booking.Payment.PaymentMethod);
    }

    [Fact]
    public async Task ConfirmOwnerBookingAsync_RecordsCashPaymentForPendingCustomerBooking()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var booking = new Booking
        {
            UserId = 2,
            CourtId = 1,
            BookingDate = VietnamClock.Today.AddDays(1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            TotalPrice = 80_000m,
            Status = BookingStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
            Payment = new Payment
            {
                Amount = 80_000m,
                PaymentMethod = "Demo",
                Status = PaymentStatus.Unpaid,
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.ConfirmOwnerBookingAsync(booking.Id, 3, 3);

        Assert.True(result.Success, result.Message);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Null(booking.ExpiresAt);
        Assert.Equal(PaymentStatus.Paid, booking.Payment!.Status);
        Assert.Equal(PaymentMethodType.CashAtVenue.ToString(), booking.Payment.PaymentMethod);
        Assert.NotNull(booking.Payment.PaidAt);

        var customerBooking = await service.GetOwnBookingAsync(booking.Id, 2);
        Assert.NotNull(customerBooking);
        Assert.True(customerBooking.IsPaid);
        Assert.Equal(PaymentStatus.Paid, customerBooking.PaymentStatus);
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
            Id = 105,
            OwnerId = 4,
            Name = "Sân của chủ khác",
            CourtTypeId = 1,
            Location = "Khu khác",
            PricePerHour = 100_000,
            Status = CourtStatus.Active
        });
        context.Bookings.Add(new Booking
        {
            CourtId = 105,
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

    [Fact]
    public async Task CreateBookingAsync_RejectsCourtAtUnapprovedFacility()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var facility = new Facility
        {
            Id = 20,
            OwnerId = 3,
            Name = "Cơ sở chờ duyệt",
            Address = "Địa chỉ kiểm thử",
            IsApproved = false,
            IsActive = true
        };
        context.Facilities.Add(facility);
        context.Courts.Add(new Court
        {
            Id = 20,
            OwnerId = 3,
            FacilityId = facility.Id,
            Name = "Sân chờ duyệt",
            CourtTypeId = 1,
            Location = "Khu A",
            PricePerHour = 80_000,
            Status = CourtStatus.Active
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = 2,
            CourtId = 20,
            BookingDate = VietnamClock.Today.AddDays(1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0)
        });

        Assert.False(result.Success);
        Assert.Contains("chưa được Admin phê duyệt", result.Message);
        Assert.Empty(await context.Bookings.ToListAsync());
    }

    [Fact]
    public async Task CreateBookingAsync_AppliesVoucherAndStoresDiscountSnapshot()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Vouchers.Add(new Voucher
        {
            OwnerId = 3,
            Code = "SAVE25",
            DiscountType = VoucherDiscountType.Percentage,
            DiscountValue = 25,
            StartDate = VietnamClock.Today,
            EndDate = VietnamClock.Today,
            UsageLimit = 1
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = 2,
            CourtId = 1,
            BookingDate = VietnamClock.Today.AddDays(1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            VoucherCode = "save25"
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal(20_000m, result.Data!.DiscountAmount);
        Assert.Equal(60_000m, result.Data.TotalPrice);
        var booking = await context.Bookings.SingleAsync();
        Assert.Equal("SAVE25", booking.VoucherCode);
        Assert.Equal(20_000m, booking.DiscountAmount);
        Assert.Equal(60_000m, booking.TotalPrice);
    }

    [Fact]
    public async Task CreateBookingAsync_RejectsVoucherAfterItsUsageLimitIsReached()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Vouchers.Add(new Voucher
        {
            OwnerId = 3,
            Code = "ONCE",
            DiscountType = VoucherDiscountType.FixedAmount,
            DiscountValue = 10_000,
            StartDate = VietnamClock.Today,
            EndDate = VietnamClock.Today.AddDays(2),
            UsageLimit = 1
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var date = VietnamClock.Today.AddDays(1);

        var first = await service.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = 2,
            CourtId = 1,
            BookingDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            VoucherCode = "ONCE"
        });
        var second = await service.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = 2,
            CourtId = 1,
            BookingDate = date,
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(12, 0),
            VoucherCode = "ONCE"
        });

        Assert.True(first.Success, first.Message);
        Assert.False(second.Success);
        Assert.Contains("hết lượt", second.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.Bookings.ToListAsync());
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
