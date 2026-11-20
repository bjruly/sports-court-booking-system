using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerVoucherServiceTests
{
    [Fact]
    public async Task CreateOwnerVoucherAsync_NormalizesCodeAndPreventsDuplicates()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = new OwnerVoucherService(context);
        var request = CreateRequest();
        request.Code = "  save25 ";

        var created = await service.CreateOwnerVoucherAsync(3, request);
        var duplicate = await service.CreateOwnerVoucherAsync(3, request);

        Assert.True(created.Success);
        Assert.False(duplicate.Success);
        Assert.Equal("SAVE25", (await context.Vouchers.SingleAsync()).Code);
    }

    [Fact]
    public async Task ValidateForCourtAsync_OnlyAppliesVoucherToCourtsOwnedByVoucherOwner()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(CreateOwner(4));
        context.Courts.Add(new Court
        {
            Id = 105,
            OwnerId = 4,
            Name = "Sân chủ sân khác",
            CourtTypeId = 1,
            Location = "Khu khác",
            PricePerHour = 80_000,
            Status = CourtStatus.Active
        });
        context.Vouchers.Add(new Voucher
        {
            OwnerId = 3,
            Code = "SAVE25",
            DiscountType = VoucherDiscountType.Percentage,
            DiscountValue = 25,
            StartDate = VietnamToday(),
            EndDate = VietnamToday().AddDays(2)
        });
        await context.SaveChangesAsync();
        var service = new OwnerVoucherService(context);

        var onOwnedCourt = await service.ValidateForCourtAsync(1, VietnamToday(), 80_000m, "save25");
        var onAnotherOwnerCourt = await service.ValidateForCourtAsync(105, VietnamToday(), 80_000m, "SAVE25");

        Assert.True(onOwnedCourt.Success);
        Assert.Equal(20_000m, onOwnedCourt.DiscountAmount);
        Assert.Equal(60_000m, onOwnedCourt.TotalPrice);
        Assert.False(onAnotherOwnerCourt.Success);
    }

    [Fact]
    public async Task ValidateForCourtAsync_ReleasesUsageFromCancelledBooking()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var voucher = new Voucher
        {
            OwnerId = 3,
            Code = "ONCE",
            DiscountType = VoucherDiscountType.FixedAmount,
            DiscountValue = 10_000,
            StartDate = VietnamToday(),
            EndDate = VietnamToday().AddDays(2),
            UsageLimit = 1
        };
        context.Vouchers.Add(voucher);
        await context.SaveChangesAsync();
        context.Bookings.Add(new Booking
        {
            UserId = 2,
            CourtId = 1,
            VoucherId = voucher.Id,
            VoucherCode = voucher.Code,
            DiscountAmount = 10_000,
            TotalPrice = 70_000,
            BookingDate = VietnamToday().AddDays(1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = BookingStatus.Cancelled
        });
        await context.SaveChangesAsync();
        var service = new OwnerVoucherService(context);

        var result = await service.ValidateForCourtAsync(1, VietnamToday().AddDays(1), 80_000m, "ONCE");

        Assert.True(result.Success);
        Assert.Equal(10_000m, result.DiscountAmount);
    }

    [Fact]
    public async Task UpdateOwnerVoucherAsync_DoesNotSetLimitBelowCurrentUsage()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var voucher = new Voucher
        {
            OwnerId = 3,
            Code = "SAVE25",
            DiscountType = VoucherDiscountType.Percentage,
            DiscountValue = 25,
            StartDate = VietnamToday(),
            EndDate = VietnamToday().AddDays(2),
            UsageLimit = 2
        };
        context.Vouchers.Add(voucher);
        await context.SaveChangesAsync();
        context.Bookings.AddRange(new[]
        {
            new Booking
            {
                UserId = 2,
                CourtId = 1,
                VoucherId = voucher.Id,
                VoucherCode = voucher.Code,
                DiscountAmount = 20_000,
                TotalPrice = 60_000,
                BookingDate = VietnamToday().AddDays(1),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 0),
                Status = BookingStatus.Confirmed
            },
            new Booking
            {
                UserId = 2,
                CourtId = 1,
                VoucherId = voucher.Id,
                VoucherCode = voucher.Code,
                DiscountAmount = 20_000,
                TotalPrice = 60_000,
                BookingDate = VietnamToday().AddDays(1),
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(12, 0),
                Status = BookingStatus.Confirmed
            }
        });
        await context.SaveChangesAsync();
        var request = CreateRequest();
        request.UsageLimit = 1;
        var service = new OwnerVoucherService(context);

        var result = await service.UpdateOwnerVoucherAsync(voucher.Id, 3, request);

        Assert.False(result.Success);
        Assert.Equal(2, (await context.Vouchers.SingleAsync()).UsageLimit);
    }

    private static OwnerVoucherRequest CreateRequest() => new()
    {
        Code = "SAVE25",
        DiscountType = VoucherDiscountType.Percentage,
        DiscountValue = 25,
        StartDate = VietnamToday(),
        EndDate = VietnamToday().AddDays(2),
        UsageLimit = 5,
        IsActive = true
    };

    private static DateOnly VietnamToday() => VietnamClock.Today;

    private static User CreateOwner(int id) => new()
    {
        Id = id,
        FullName = $"Chủ sân {id}",
        Email = $"owner{id}@example.com",
        PhoneNumber = "0900000000",
        PasswordHash = "test-hash",
        Role = UserRole.CourtOwner,
        IsActive = true
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
