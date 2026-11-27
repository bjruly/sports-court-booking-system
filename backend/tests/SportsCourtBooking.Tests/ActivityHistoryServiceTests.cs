using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class ActivityHistoryServiceTests
{
    [Fact]
    public async Task HistoryCombinesRoleAndVietnamDateAndRequiresAnActiveAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var admin = await context.Users.SingleAsync(user => user.Role == UserRole.Admin);
        var owner = await context.Users.SingleAsync(user => user.Role == UserRole.CourtOwner);
        var date = new DateOnly(2026, 11, 27);
        var startsAt = new DateTime(2026, 11, 26, 17, 0, 0, DateTimeKind.Utc);
        var endsAt = startsAt.AddDays(1);

        for (var index = 1; index <= 27; index++)
        {
            AddActivity(admin, startsAt, $"Thao tác {index}");
        }
        AddActivity(owner, endsAt.AddSeconds(-1), "Thao tác chủ sân");
        AddActivity(admin, endsAt, "Ngày tiếp theo");
        AddActivity(owner, startsAt.AddSeconds(-1), "Ngày trước");
        await context.SaveChangesAsync();

        var service = new ActivityHistoryService(context);
        var firstPage = await service.GetPageAsync(admin.Id, null, date, 1);
        var lastPage = await service.GetPageAsync(admin.Id, null, date, 99);
        var ownerPage = await service.GetPageAsync(admin.Id, UserRole.CourtOwner, date, 1);
        var adminPage = await service.GetPageAsync(admin.Id, UserRole.Admin, date, 1);

        Assert.Equal(2, firstPage.PageCount);
        Assert.Equal(25, firstPage.Items.Count);
        Assert.Equal("Thao tác chủ sân", firstPage.Items[0].Action);
        Assert.Equal(2, lastPage.Page);
        Assert.Equal(new[] { "Thao tác 3", "Thao tác 2", "Thao tác 1" }, lastPage.Items.Select(item => item.Action));
        Assert.Equal(owner.FullName, Assert.Single(ownerPage.Items).ActorName);
        Assert.Equal(UserRole.CourtOwner, ownerPage.Items[0].ActorRole);
        Assert.All(adminPage.Items, item => Assert.Equal(admin.Id, item.ActorId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPageAsync(owner.Id, null, null, 1));

        admin.IsActive = false;
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPageAsync(admin.Id, null, null, 1));

        void AddActivity(User actor, DateTime time, string action) => context.ActivityRecords.Add(new ActivityRecord
        {
            ActorId = actor.Id,
            ActorRole = actor.Role,
            Action = action,
            Target = "Đối tượng kiểm thử",
            OccurredAtUtc = time
        });
    }
}
