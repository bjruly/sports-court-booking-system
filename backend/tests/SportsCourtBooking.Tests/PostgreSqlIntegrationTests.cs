using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Tests;

public class PostgreSqlIntegrationTests
{
    [PostgreSqlFact]
    public async Task ConcurrentBookingsAllowOnlyOneReservation()
    {
        await using var setup = CreateContext();
        var fixture = await SeedCourtAsync(setup);

        try
        {
            var tasks = fixture.CustomerIds.Select(async customerId =>
            {
                await using var context = CreateContext();
                var service = CreateBookingService(context);
                return await service.CreateBookingAsync(new CreateBookingRequest
                {
                    UserId = customerId,
                    CourtId = fixture.CourtId,
                    BookingDate = VietnamClock.Today.AddDays(1),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0)
                });
            });

            var results = await Task.WhenAll(tasks);

            Assert.Single(results.Where(result => result.Success));
            Assert.Equal(fixture.CustomerIds.Length - 1,
                results.Count(result => !result.Success));
            Assert.All(results.Where(result => !result.Success),
                result => Assert.Contains("đã có người đặt", result.Message));
        }
        finally
        {
            await CleanupAsync(setup, fixture);
        }
    }

    [PostgreSqlFact]
    public async Task ExpiredPendingBookingIsMarkedExpiredAndReleasesTimeSlot()
    {
        await using var setup = CreateContext();
        var fixture = await SeedCourtAsync(setup);
        var bookingDate = VietnamClock.Today.AddDays(1);

        try
        {
            setup.Bookings.Add(new Booking
            {
                UserId = fixture.CustomerIds[0],
                CourtId = fixture.CourtId,
                BookingDate = bookingDate,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(15, 0),
                TotalPrice = 80_000m,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-11),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });
            await setup.SaveChangesAsync();

            var expirationService = new PendingBookingExpirationService(setup);
            var expiredCount = await expirationService.ExpireOverdueAsync();

            Assert.Equal(1, expiredCount);

            setup.ChangeTracker.Clear();
            var booking = await setup.Bookings.SingleAsync(
                item => item.CourtId == fixture.CourtId && item.BookingDate == bookingDate);
            Assert.Equal(BookingStatus.Expired, booking.Status);

            var availabilityService = CreateBookingService(setup);
            Assert.True(await availabilityService.IsTimeSlotAvailableAsync(
                fixture.CourtId,
                bookingDate,
                new TimeOnly(14, 0),
                new TimeOnly(15, 0)));
        }
        finally
        {
            await CleanupAsync(setup, fixture);
        }
    }

    [PostgreSqlFact]
    public async Task SuccessfulAdminAndOwnerActionsAppearInOneHistory()
    {
        await using var context = CreateContext();
        var fixture = await SeedCourtAsync(context);
        var adminId = await context.Users.Where(user => user.Role == UserRole.Admin)
            .Select(user => user.Id).FirstAsync();
        var booking = new Booking
        {
            UserId = fixture.CustomerIds[0],
            CourtId = fixture.CourtId,
            BookingDate = VietnamClock.Today.AddDays(1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            TotalPrice = 80_000m,
            Status = BookingStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Payment = new Payment { Amount = 80_000m, Status = PaymentStatus.Paid }
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        var target = $"Đơn #{booking.Id}";
        var courtTarget = $"{target} · Sân #{fixture.CourtId}";
        var activity = context.ActivityRecords.Where(record => record.Target == target || record.Target == courtTarget);

        try
        {
            await using var mutationContext = CreateContext();
            var service = CreateBookingService(mutationContext);
            Assert.True((await service.ConfirmBookingAsync(booking.Id, adminId)).Success);
            Assert.False((await service.ConfirmBookingAsync(booking.Id, adminId)).Success);
            Assert.True((await service.CancelOwnerBookingAsync(booking.Id, fixture.OwnerId, fixture.OwnerId)).Success);

            var records = await activity.OrderBy(item => item.Id).ToListAsync();
            Assert.Collection(records,
                record =>
                {
                    Assert.Equal(adminId, record.ActorId);
                    Assert.Equal(UserRole.Admin, record.ActorRole);
                    Assert.Equal("Xác nhận đơn đặt sân", record.Action);
                },
                record =>
                {
                    Assert.Equal(fixture.OwnerId, record.ActorId);
                    Assert.Equal(UserRole.CourtOwner, record.ActorRole);
                    Assert.Equal("Hủy đơn và tạo yêu cầu hoàn tiền", record.Action);
                });

            var date = DateOnly.FromDateTime(VietnamClock.ToVietnamTime(records[0].OccurredAtUtc));
            var history = new ActivityHistoryService(context);
            var page = await history.GetPageAsync(adminId, UserRole.Admin, date, 1);
            Assert.Contains(page.Items, item => item.Id == records[0].Id && item.ActorName is not null);
            Assert.DoesNotContain(page.Items, item => item.Id == records[1].Id);
        }
        finally
        {
            await activity.ExecuteDeleteAsync();
            var paymentId = booking.Payment!.Id;
            await context.Refunds.Where(refund => refund.PaymentId == paymentId).ExecuteDeleteAsync();
            await CleanupAsync(context, fixture);
        }
    }

    private static AppDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "SPORTS_COURT_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set SPORTS_COURT_TEST_CONNECTION to an isolated PostgreSQL test database before enabling integration tests.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static BookingService CreateBookingService(AppDbContext context)
    {
        var expirationService = new PendingBookingExpirationService(context);
        return new BookingService(
            context,
            NullLogger<BookingService>.Instance,
            expirationService);
    }

    private static async Task<IntegrationFixture> SeedCourtAsync(AppDbContext context)
    {
        await context.EnsureBookingSchemaAsync();

        var suffix = Guid.NewGuid().ToString("N");
        var owner = new User
        {
            FullName = $"Integration owner {suffix}",
            Email = $"integration-owner-{suffix}@example.test",
            PhoneNumber = "0900000001",
            PasswordHash = "test-hash",
            Role = UserRole.CourtOwner,
            IsActive = true
        };
        var customers = new[]
        {
            new User
            {
                FullName = "Integration customer 1",
                Email = $"integration-customer-1-{suffix}@example.test",
                PhoneNumber = "0900000002",
                PasswordHash = "test-hash",
                Role = UserRole.Customer,
                IsActive = true
            },
            new User
            {
                FullName = "Integration customer 2",
                Email = $"integration-customer-2-{suffix}@example.test",
                PhoneNumber = "0900000003",
                PasswordHash = "test-hash",
                Role = UserRole.Customer,
                IsActive = true
            }
        };

        context.Users.Add(owner);
        context.Users.AddRange(customers);
        await context.SaveChangesAsync();

        var court = new Court
        {
            Name = $"Integration court {suffix}",
            OwnerId = owner.Id,
            CourtTypeId = 1,
            Location = "Integration test",
            PricePerHour = 80_000m,
            Status = CourtStatus.Active
        };
        context.Courts.Add(court);
        await context.SaveChangesAsync();

        return new IntegrationFixture(
            owner.Id,
            court.Id,
            customers.Select(customer => customer.Id).ToArray());
    }

    private static async Task CleanupAsync(
        AppDbContext context,
        IntegrationFixture fixture)
    {
        var bookings = await context.Bookings
            .Where(booking => booking.CourtId == fixture.CourtId)
            .ToListAsync();
        if (bookings.Count > 0)
        {
            context.Bookings.RemoveRange(bookings);
        }

        var court = await context.Courts.FindAsync(fixture.CourtId);
        if (court is not null)
        {
            context.Courts.Remove(court);
        }

        var users = await context.Users
            .Where(user =>
                user.Id == fixture.OwnerId ||
                fixture.CustomerIds.Contains(user.Id))
            .ToListAsync();
        context.Users.RemoveRange(users);

        await context.SaveChangesAsync();
    }

    private sealed record IntegrationFixture(
        int OwnerId,
        int CourtId,
        int[] CustomerIds);
}

// Report disabled database tests as skipped instead of passing without executing assertions.
public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS"),
                "1", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_POSTGRES_INTEGRATION_TESTS=1 and SPORTS_COURT_TEST_CONNECTION to an isolated PostgreSQL test database.";
        }
    }
}
