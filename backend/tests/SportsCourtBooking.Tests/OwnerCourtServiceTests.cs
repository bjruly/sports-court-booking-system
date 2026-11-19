using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerCourtServiceTests
{
    [Fact]
    public async Task GetOwnerCourtsAsync_ReturnsOnlyCourtsOwnedByRequestedOwner()
    {
        await using var context = CreateContext();
        context.Database.EnsureCreated();
        context.Users.Add(CreateOwner(4));
        context.Courts.Add(CreateCourt(5, 4));
        await context.SaveChangesAsync();

        var service = new OwnerCourtService(context);
        var courts = await service.GetOwnerCourtsAsync(4);

        var court = Assert.Single(courts);
        Assert.Equal(5, court.Id);
        Assert.Equal("Sân chủ sân 4", court.Name);
        Assert.Null(court.FacilityId);
        Assert.Null(court.FacilityName);
    }

    [Fact]
    public async Task UpdateOwnerCourtAsync_DoesNotUpdateCourtOwnedByAnotherOwner()
    {
        await using var context = CreateContext();
        context.Database.EnsureCreated();
        var service = new OwnerCourtService(context);
        context.Facilities.Add(CreateFacility(1, 3));
        await context.SaveChangesAsync();

        var result = await service.UpdateOwnerCourtAsync(1, 4, new OwnerCourtRequest
        {
            Name = "Tên bị sửa trái phép",
            FacilityId = 1,
            CourtTypeId = 1,
            Location = "Địa điểm mới",
            PricePerHour = 100_000,
            Status = CourtStatus.Active
        });

        Assert.False(result.Success);
        var unchangedCourt = await context.Courts.SingleAsync(court => court.Id == 1);
        Assert.Equal(3, unchangedCourt.OwnerId);
        Assert.Equal("Sân Cầu Lông VIP 1", unchangedCourt.Name);
    }

    [Fact]
    public async Task CreateOwnerCourtAsync_AssignsAuthenticatedOwnerId()
    {
        await using var context = CreateContext();
        context.Database.EnsureCreated();
        context.Users.Add(CreateOwner(4));
        context.Facilities.Add(CreateFacility(2, 4));
        await context.SaveChangesAsync();
        var service = new OwnerCourtService(context);

        var result = await service.CreateOwnerCourtAsync(4, new OwnerCourtRequest
        {
            Name = "Sân chủ sân 4",
            FacilityId = 2,
            CourtTypeId = 1,
            Location = "Khu D",
            PricePerHour = 100_000,
            Status = CourtStatus.Active
        });

        Assert.True(result.Success);
        var createdCourt = await context.Courts.SingleAsync(court => court.Name == "Sân chủ sân 4");
        Assert.Equal(4, createdCourt.OwnerId);
        Assert.Equal(2, createdCourt.FacilityId);
    }

    [Fact]
    public async Task CreateOwnerCourtAsync_RejectsFacilityOwnedByAnotherOwner()
    {
        await using var context = CreateContext();
        context.Database.EnsureCreated();
        context.Users.Add(CreateOwner(4));
        context.Facilities.Add(CreateFacility(2, 3));
        await context.SaveChangesAsync();
        var service = new OwnerCourtService(context);

        var result = await service.CreateOwnerCourtAsync(4, new OwnerCourtRequest
        {
            Name = "Sân chủ sân 4",
            FacilityId = 2,
            CourtTypeId = 1,
            Location = "Khu D",
            PricePerHour = 100_000,
            Status = CourtStatus.Active
        });

        Assert.False(result.Success);
        Assert.Empty(await context.Courts.Where(court => court.OwnerId == 4).ToListAsync());
    }

    [Fact]
    public async Task UpdateOwnerCourtAsync_SavesPerCourtPeakPricing()
    {
        await using var context = CreateContext();
        context.Database.EnsureCreated();
        var service = new OwnerCourtService(context);
        context.Facilities.Add(CreateFacility(1, 3));
        await context.SaveChangesAsync();

        var result = await service.UpdateOwnerCourtAsync(1, 3, new OwnerCourtRequest
        {
            Name = "Sân Cầu Lông VIP 1",
            FacilityId = 1,
            CourtTypeId = 1,
            Location = "Khu A - Tầng 1",
            PricePerHour = 80_000,
            PeakStartHour = 18,
            PeakSurcharge = 25_000,
            Status = CourtStatus.Active
        });

        Assert.True(result.Success);
        var updatedCourt = await context.Courts.SingleAsync(court => court.Id == 1);
        Assert.Equal(18, updatedCourt.PeakStartHour);
        Assert.Equal(25_000m, updatedCourt.PeakSurcharge);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

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

    private static Court CreateCourt(int id, int ownerId) => new()
    {
        Id = id,
        OwnerId = ownerId,
        Name = $"Sân chủ sân {ownerId}",
        CourtTypeId = 1,
        Location = "Khu D",
        PricePerHour = 100_000,
        Status = CourtStatus.Active
    };

    private static Facility CreateFacility(int id, int ownerId) => new()
    {
        Id = id,
        OwnerId = ownerId,
        Name = $"Cơ sở {id}",
        Address = "Địa chỉ"
    };
}
