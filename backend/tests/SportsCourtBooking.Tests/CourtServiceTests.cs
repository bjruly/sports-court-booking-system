using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class CourtServiceTests
{
    [Fact]
    public async Task GetAllCourtsAsync_OnlyReturnsCourtsAtApprovedActiveFacilities()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(CreateOwner());
        context.Facilities.AddRange(
            CreateFacility(10, isApproved: false, isActive: true),
            CreateFacility(11, isApproved: true, isActive: true),
            CreateFacility(12, isApproved: true, isActive: false));
        context.Courts.AddRange(
            CreateCourt(110, facilityId: 10),
            CreateCourt(111, facilityId: 11),
            CreateCourt(112, facilityId: 12),
            CreateCourt(113, facilityId: null));
        await context.SaveChangesAsync();
        var service = new CourtService(context);

        var courts = await service.GetAllCourtsAsync(onlyActive: true);

        var courtIds = courts.Select(court => court.Id).ToArray();
        Assert.Contains(111, courtIds);
        Assert.Contains(113, courtIds);
        Assert.DoesNotContain(110, courtIds);
        Assert.DoesNotContain(112, courtIds);
    }

    [Fact]
    public async Task GetCourtByIdAsync_HidesCourtAtUnapprovedFacility()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(CreateOwner());
        context.Facilities.Add(CreateFacility(10, isApproved: false, isActive: true));
        context.Courts.Add(CreateCourt(110, facilityId: 10));
        await context.SaveChangesAsync();
        var service = new CourtService(context);

        var court = await service.GetCourtByIdAsync(110);

        Assert.Null(court);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static User CreateOwner() => new()
    {
        Id = 20,
        FullName = "Chủ sân kiểm thử",
        Email = "owner-court-service@example.com",
        PhoneNumber = "0900000020",
        PasswordHash = "test-hash",
        Role = UserRole.CourtOwner
    };

    private static Facility CreateFacility(int id, bool isApproved, bool isActive) => new()
    {
        Id = id,
        OwnerId = 20,
        Name = $"Cơ sở {id}",
        Address = "Địa chỉ kiểm thử",
        IsApproved = isApproved,
        IsActive = isActive
    };

    private static Court CreateCourt(int id, int? facilityId) => new()
    {
        Id = id,
        OwnerId = 20,
        FacilityId = facilityId,
        Name = $"Sân {id}",
        CourtTypeId = 1,
        Location = "Khu A",
        PricePerHour = 100_000,
        Status = CourtStatus.Active
    };
}
