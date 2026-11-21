using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Security;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerProfileServiceTests
{
    [Fact]
    public async Task UpdateOwnerProfileAsync_UpdatesOnlyNameAndPhoneForOwner()
    {
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = 3,
            FullName = "Chủ Sân",
            Email = "owner@example.com",
            PhoneNumber = "0901111111",
            PasswordHash = "unchanged-hash",
            Role = UserRole.CourtOwner
        });
        await context.SaveChangesAsync();

        var result = await new UserService(context, new Pbkdf2PasswordHasher()).UpdateOwnerProfileAsync(3, new OwnerProfileUpdateRequest
        {
            FullName = "  Chủ Sân Mới  ",
            PhoneNumber = "  +84901234567 "
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal("Chủ Sân Mới", result.Profile?.FullName);
        Assert.Equal("owner@example.com", result.Profile?.Email);
        Assert.Equal("+84901234567", result.Profile?.PhoneNumber);
        Assert.Equal("unchanged-hash", (await context.Users.SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task UpdateOwnerProfileAsync_RejectsNonOwnerAndInvalidPhone()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User
            {
                Id = 2,
                FullName = "Khách hàng",
                Email = "customer@example.com",
                PhoneNumber = "0901234567",
                PasswordHash = "hash",
                Role = UserRole.Customer
            },
            new User
            {
                Id = 3,
                FullName = "Chủ Sân",
                Email = "owner@example.com",
                PhoneNumber = "0901111111",
                PasswordHash = "hash",
                Role = UserRole.CourtOwner
            });
        await context.SaveChangesAsync();
        var service = new UserService(context, new Pbkdf2PasswordHasher());

        var nonOwnerResult = await service.UpdateOwnerProfileAsync(2, new OwnerProfileUpdateRequest
        {
            FullName = "Tên mới",
            PhoneNumber = "0901234567"
        });
        var invalidPhoneResult = await service.UpdateOwnerProfileAsync(3, new OwnerProfileUpdateRequest
        {
            FullName = "Tên mới",
            PhoneNumber = "123"
        });

        Assert.False(nonOwnerResult.Success);
        Assert.False(invalidPhoneResult.Success);
        Assert.Equal("Khách hàng", (await context.Users.SingleAsync(user => user.Id == 2)).FullName);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
