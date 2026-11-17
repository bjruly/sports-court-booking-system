using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class OwnerPasswordServiceTests
{
    [Fact]
    public async Task ChangeOwnerPasswordAsync_UpdatesHashWhenCurrentPasswordIsCorrect()
    {
        await using var context = CreateContext();
        var hasher = new TestPasswordHasher();
        context.Users.Add(CreateUser(3, UserRole.CourtOwner, hasher.Hash("OldPassword1")));
        await context.SaveChangesAsync();

        var result = await new UserService(context, hasher).ChangeOwnerPasswordAsync(3, new OwnerChangePasswordRequest
        {
            CurrentPassword = "OldPassword1",
            NewPassword = "NewPassword2"
        });

        var updatedUser = await context.Users.SingleAsync();
        Assert.True(result.Success, result.Message);
        Assert.True(hasher.Verify("NewPassword2", updatedUser.PasswordHash));
        Assert.False(hasher.Verify("OldPassword1", updatedUser.PasswordHash));
        Assert.NotEqual(default, updatedUser.UpdatedAt);
    }

    [Fact]
    public async Task ChangeOwnerPasswordAsync_RejectsWrongPasswordAndNonOwner()
    {
        await using var context = CreateContext();
        var hasher = new TestPasswordHasher();
        var storedHash = hasher.Hash("OldPassword1");
        context.Users.AddRange(
            CreateUser(2, UserRole.Customer, storedHash),
            CreateUser(3, UserRole.CourtOwner, storedHash));
        await context.SaveChangesAsync();
        var service = new UserService(context, hasher);

        var wrongPassword = await service.ChangeOwnerPasswordAsync(3, new OwnerChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword2"
        });
        var nonOwner = await service.ChangeOwnerPasswordAsync(2, new OwnerChangePasswordRequest
        {
            CurrentPassword = "OldPassword1",
            NewPassword = "NewPassword2"
        });

        Assert.False(wrongPassword.Success);
        Assert.Contains("không chính xác", wrongPassword.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(nonOwner.Success);
        Assert.Equal(storedHash, (await context.Users.SingleAsync(user => user.Id == 3)).PasswordHash);
    }

    [Fact]
    public async Task ChangeOwnerPasswordAsync_RejectsWeakOrUnchangedPassword()
    {
        await using var context = CreateContext();
        var hasher = new TestPasswordHasher();
        var storedHash = hasher.Hash("OldPassword1");
        context.Users.Add(CreateUser(3, UserRole.CourtOwner, storedHash));
        await context.SaveChangesAsync();
        var service = new UserService(context, hasher);

        var weakPassword = await service.ChangeOwnerPasswordAsync(3, new OwnerChangePasswordRequest
        {
            CurrentPassword = "OldPassword1",
            NewPassword = "short"
        });
        var unchangedPassword = await service.ChangeOwnerPasswordAsync(3, new OwnerChangePasswordRequest
        {
            CurrentPassword = "OldPassword1",
            NewPassword = "OldPassword1"
        });

        Assert.False(weakPassword.Success);
        Assert.False(unchangedPassword.Success);
        Assert.Equal(storedHash, (await context.Users.SingleAsync()).PasswordHash);
    }

    private static User CreateUser(int id, UserRole role, string passwordHash) => new()
    {
        Id = id,
        FullName = role == UserRole.CourtOwner ? "Chủ sân" : "Khách hàng",
        Email = $"user{id}@example.com",
        PhoneNumber = "0901234567",
        PasswordHash = passwordHash,
        Role = role
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"test-hash:{password}";

        public bool Verify(string password, string storedHash)
            => string.Equals(Hash(password), storedHash, StringComparison.Ordinal);
    }
}
