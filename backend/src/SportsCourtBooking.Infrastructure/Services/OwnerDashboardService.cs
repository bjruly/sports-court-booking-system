using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class OwnerDashboardService : IOwnerDashboardService
{
    private readonly AppDbContext _context;

    public OwnerDashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OwnerDashboardDto> GetDashboardAsync(int ownerId)
    {
        var ownedCourts = _context.Courts.Where(court => court.OwnerId == ownerId);
        var ownedBookings = _context.Bookings.Where(booking => booking.Court!.OwnerId == ownerId);
        var today = VietnamClock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var recentBookings = await ownedBookings
            .AsNoTracking()
            .OrderByDescending(booking => booking.CreatedAt)
            .Take(5)
            .Select(booking => new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                CustomerName = booking.User != null ? booking.User.FullName : booking.GuestName ?? string.Empty,
                CustomerPhone = booking.User != null ? booking.User.PhoneNumber : booking.GuestPhone ?? string.Empty,
                CourtId = booking.CourtId,
                CourtName = booking.Court!.Name,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status
            })
            .ToListAsync();

        var monthlyRevenue = await ownedBookings
            .Where(booking =>
                (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Completed) &&
                booking.BookingDate >= monthStart &&
                booking.BookingDate < nextMonthStart)
            .Select(booking => (decimal?)booking.TotalPrice)
            .SumAsync() ?? 0m;

        return new OwnerDashboardDto
        {
            TotalCourts = await ownedCourts.CountAsync(),
            ActiveCourts = await ownedCourts.CountAsync(court => court.Status == CourtStatus.Active),
            MaintenanceCourts = await ownedCourts.CountAsync(court => court.Status == CourtStatus.Maintenance),
            InactiveCourts = await ownedCourts.CountAsync(court => court.Status == CourtStatus.Inactive),
            TodayBookings = await ownedBookings.CountAsync(booking =>
                booking.BookingDate == today &&
                booking.Status != BookingStatus.Cancelled &&
                booking.Status != BookingStatus.Expired),
            PendingBookings = await ownedBookings.CountAsync(booking => booking.Status == BookingStatus.Pending),
            MonthlyRevenue = monthlyRevenue,
            RecentBookings = recentBookings
        };
    }
}
