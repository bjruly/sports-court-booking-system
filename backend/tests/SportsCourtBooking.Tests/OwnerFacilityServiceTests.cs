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
        context.CourtTypes.Add(new CourtType { Id = 1, Name = "Cầu lông" });
        await context.SaveChangesAsync();
        var service = new OwnerFacilityService(context);

        var result = await service.CreateOwnerFacilityAsync(3, new OwnerFacilityRequest
        {
            Name = "  Cơ sở Trung tâm  ",
            Address = "  123 Đường A  ",
            Description = "  Sân trong nhà  ",
            CourtTypeId = 1,
            PhoneNumber = "0901234567"
        });

        Assert.True(result.Success, result.Message);
        var facility = Assert.Single(await service.GetOwnerFacilitiesAsync(3));
        Assert.Equal("Cơ sở Trung tâm", facility.Name);
        Assert.Equal("123 Đường A", facility.Address);
        Assert.Equal("Sân trong nhà", facility.Description);
        Assert.False(facility.IsApproved);
        Assert.True(facility.IsActive);
        Assert.Empty(await service.GetOwnerFacilitiesAsync(4));
        var courts = await context.Courts.Where(court => court.FacilityId == facility.Id).ToListAsync();
        Assert.Equal(3, courts.Count);
        Assert.All(courts, court =>
        {
            Assert.Equal(3, court.OwnerId);
            Assert.Equal(1, court.CourtTypeId);
            Assert.Equal(CourtStatus.Inactive, court.Status);
        });

        var adminService = new FacilityService(context);
        var pendingForAdmin = await adminService.GetAllFacilitiesAsync(filter: "pending");
        Assert.Contains(pendingForAdmin, item => item.Id == facility.Id);

        var approval = await adminService.ApproveFacilityAsync(facility.Id);
        Assert.True(approval.Success, approval.Message);
        var approvedFacility = await service.GetOwnerFacilityAsync(facility.Id, 3);
        Assert.True(approvedFacility?.IsApproved);

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

    [Fact]
    public async Task DeleteOwnerFacilityAsync_DeletesOnlyEmptyFacilitiesOwnedByTheRequester()
    {
        await using var context = CreateContext();
        context.Users.AddRange(CreateOwner(3), CreateOwner(4));
        context.Facilities.AddRange(
            new Facility
            {
                Id = 1,
                OwnerId = 3,
                Name = "Cơ sở trống",
                Address = "Địa chỉ 1"
            },
            new Facility
            {
                Id = 2,
                OwnerId = 3,
                Name = "Cơ sở đang dùng",
                Address = "Địa chỉ 2"
            });
        context.Courts.Add(new Court
        {
            Id = 10,
            OwnerId = 3,
            FacilityId = 2,
            Name = "Sân đang dùng",
            CourtTypeId = 1,
            Location = "Khu A",
            PricePerHour = 100_000
        });
        await context.SaveChangesAsync();
        var service = new OwnerFacilityService(context);

        var otherOwner = await service.DeleteOwnerFacilityAsync(1, 4);
        var linkedFacility = await service.DeleteOwnerFacilityAsync(2, 3);
        var emptyFacility = await service.DeleteOwnerFacilityAsync(1, 3);

        Assert.False(otherOwner.Success);
        Assert.False(linkedFacility.Success);
        Assert.Contains("đang có sân", linkedFacility.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(emptyFacility.Success, emptyFacility.Message);
        Assert.Null(await context.Facilities.FindAsync(1));
        Assert.NotNull(await context.Facilities.FindAsync(2));
        Assert.Equal(2, (await context.Courts.SingleAsync()).FacilityId);
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
