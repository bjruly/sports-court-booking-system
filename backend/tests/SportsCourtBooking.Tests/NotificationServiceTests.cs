using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task OwnerOnlySeesBookingsOnOwnCourtsWithWebLink()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var items = await new NotificationService(context).GetNotificationsAsync(10, UserRole.CourtOwner);

        var item = Assert.Single(items);
        Assert.Equal("booking_1", item.Id);
        Assert.Equal("/Owner/Bookings", item.Url);
        // Giờ chơi buổi tối phải hiển thị theo 24 giờ.
        Assert.Contains("18:00-19:00", item.Message);
    }

    [Fact]
    public async Task AdminSeesBookingsOfAllOwnersWithDesktopScreenKey()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var items = await new NotificationService(context).GetNotificationsAsync(1, UserRole.Admin);

        Assert.Equal(2, items.Count(item => item.Type == "booking"));
        Assert.All(items, item => Assert.DoesNotContain('/', item.Url));
    }

    [Fact]
    public async Task CustomerOnlySeesOwnBookingsWithWebLink()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var items = await new NotificationService(context).GetNotificationsAsync(20, UserRole.Customer);

        Assert.Equal("/Booking/Details/1", Assert.Single(items).Url);
    }

    [Fact]
    public void SharedRegistrationResolvesEveryService()
    {
        using var provider = new ServiceCollection()
            .AddSportsCourtServices("Host=localhost;Database=unused")
            .AddLogging()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBookingService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<INotificationService>());
    }

    private static AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedAsync(AppDbContext context)
    {
        context.Users.AddRange(
            CreateUser(10, UserRole.CourtOwner),
            CreateUser(11, UserRole.CourtOwner),
            CreateUser(20, UserRole.Customer),
            CreateUser(21, UserRole.Customer));
        context.CourtTypes.Add(new CourtType { Id = 1, Name = "Cầu lông" });
        context.Courts.AddRange(
            new Court { Id = 1, OwnerId = 10, CourtTypeId = 1, Name = "Sân A" },
            new Court { Id = 2, OwnerId = 11, CourtTypeId = 1, Name = "Sân B" });
        context.Bookings.AddRange(CreateBooking(1, 1, 20), CreateBooking(2, 2, 21));
        await context.SaveChangesAsync();
    }

    private static User CreateUser(int id, UserRole role) => new()
    {
        Id = id, FullName = $"Người dùng {id}", Email = $"user{id}@test.vn", PhoneNumber = "0900000000",
        PasswordHash = "x", Role = role, IsActive = true
    };

    private static Booking CreateBooking(int id, int courtId, int userId) => new()
    {
        Id = id, CourtId = courtId, UserId = userId, Status = BookingStatus.Confirmed, CreatedAt = DateTime.UtcNow,
        BookingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
        StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), TotalPrice = 100_000m
    };
}
