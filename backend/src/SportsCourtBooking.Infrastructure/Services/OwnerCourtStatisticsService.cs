using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class OwnerCourtStatisticsService : IOwnerCourtStatisticsService
{
    private readonly AppDbContext _context;

    public OwnerCourtStatisticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OwnerCourtStatisticsDto> GetMonthlyStatisticsAsync(int ownerId, int year, int month)
    {
        if (year < 1 || year > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var courts = await _context.Courts
            .AsNoTracking()
            .Where(court => court.OwnerId == ownerId)
            .OrderBy(court => court.Name)
            .Select(court => new
            {
                court.Id,
                court.Name,
                court.Status
            })
            .ToListAsync();

        var bookingStatistics = await _context.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Court!.OwnerId == ownerId
                && booking.BookingDate >= monthStart
                && booking.BookingDate < nextMonthStart)
            .GroupBy(booking => booking.CourtId)
            .Select(group => new
            {
                CourtId = group.Key,
                TotalBookings = group.Count(),
                SuccessfulBookings = group.Count(booking =>
                    booking.Status == BookingStatus.Confirmed
                    || booking.Status == BookingStatus.Completed),
                PendingBookings = group.Count(booking => booking.Status == BookingStatus.Pending),
                CancelledBookings = group.Count(booking => booking.Status == BookingStatus.Cancelled),
                ExpiredBookings = group.Count(booking => booking.Status == BookingStatus.Expired)
            })
            .ToDictionaryAsync(statistics => statistics.CourtId);

        var statisticsRows = courts
            .Select(court =>
            {
                bookingStatistics.TryGetValue(court.Id, out var bookings);
                return new OwnerCourtStatisticsRowDto
                {
                    CourtId = court.Id,
                    CourtName = court.Name,
                    Status = court.Status,
                    TotalBookings = bookings?.TotalBookings ?? 0,
                    SuccessfulBookings = bookings?.SuccessfulBookings ?? 0,
                    PendingBookings = bookings?.PendingBookings ?? 0,
                    CancelledBookings = bookings?.CancelledBookings ?? 0,
                    ExpiredBookings = bookings?.ExpiredBookings ?? 0
                };
            })
            .ToList();

        return new OwnerCourtStatisticsDto
        {
            MonthStart = monthStart,
            TotalCourts = statisticsRows.Count,
            ActiveCourts = statisticsRows.Count(court => court.Status == CourtStatus.Active),
            TotalBookings = statisticsRows.Sum(court => court.TotalBookings),
            SuccessfulBookings = statisticsRows.Sum(court => court.SuccessfulBookings),
            PendingBookings = statisticsRows.Sum(court => court.PendingBookings),
            CancelledBookings = statisticsRows.Sum(court => court.CancelledBookings),
            ExpiredBookings = statisticsRows.Sum(court => court.ExpiredBookings),
            Courts = statisticsRows
        };
    }
}
