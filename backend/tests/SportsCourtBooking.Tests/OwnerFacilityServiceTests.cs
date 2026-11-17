using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerFacilityServiceTests
{
    [Fact]
    public async Task CreateOwnerFacilityAsync_TrimsFieldsAndListsOnlyOwnerFacilities()
    {
        await using var context = CreateContext();
        context.Users.AddRange(CreateOwner(3), CreateOwner(4));
        await context.SaveChangesAsync();
        var service = new OwnerFacilityService(context);

        var result = await service.CreateOwnerFacilityAsync(3, new OwnerFacilityRequest
        {
            Name = "  Cơ sở Trung tâm  ",
            Address = "  123 Đường A  ",
            Description = "  Sân trong nhà  "
        });

        var facility = Assert.Single(await service.GetOwnerFacilitiesAsync(3));
        Assert.True(result.Success, result.Message);
        Assert.Equal("Cơ sở Trung tâm", facility.Name);
        Assert.Equal("123 Đường A", facility.Address);
        Assert.Equal("Sân trong nhà", facility.Description);
        Assert.Empty(await service.GetOwnerFacilitiesAsync(4));

        var updateResult = await service.UpdateOwnerFacilityAsync(facility.Id, 3, new OwnerFacilityRequest
        {
            Name = "Cơ sở mới",
            Address = "456 Đường B"
        });

        Assert.True(updateResult.Success, updateResult.Message);
        var updatedFacility = await service.GetOwnerFacilityAsync(facility.Id, 3);
        Assert.Equal("Cơ sở mới", updatedFacility?.Name);
        Assert.Equal("456 Đường B", updatedFacility?.Address);
    }

    [Fact]
    public async Task UpdateOwnerFacilityAsync_RejectsFacilityOwnedByAnotherOwner()
    {
        await using var context = CreateContext();
        context.Users.AddRange(CreateOwner(3), CreateOwner(4));
        context.Facilities.Add(new Facility
        {
            Id = 1,
            OwnerId = 3,
            Name = "Cơ sở 1",
            Address = "Địa chỉ cũ"
        });
        await context.SaveChangesAsync();
        var service = new OwnerFacilityService(context);

        var result = await service.UpdateOwnerFacilityAsync(1, 4, new OwnerFacilityRequest
        {
            Name = "Cơ sở mới",
            Address = "Địa chỉ mới"
        });

        Assert.False(result.Success);
        Assert.Equal("Địa chỉ cũ", (await context.Facilities.SingleAsync()).Address);
        Assert.Null(await service.GetOwnerFacilityAsync(1, 4));
    }

    [Fact]
    public async Task CreateOwnerFacilityAsync_RejectsInvalidDetails()
    {
        await using var context = CreateContext();
        context.Users.Add(CreateOwner(3));
        await context.SaveChangesAsync();
        var service = new OwnerFacilityService(context);

        var result = await service.CreateOwnerFacilityAsync(3, new OwnerFacilityRequest
        {
            Name = " ",
            Address = " "
        });

        Assert.False(result.Success);
        Assert.Empty(await context.Facilities.ToListAsync());
    }

    private static User CreateOwner(int id) => new()
    {
        Id = id,
        FullName = $"Chủ sân {id}",
        Email = $"owner{id}@example.com",
        PhoneNumber = "0900000000",
        PasswordHash = "test-hash",
        Role = UserRole.CourtOwner
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
