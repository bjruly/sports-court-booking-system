using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class OwnerRevenueReportService : IOwnerRevenueReportService
{
    private readonly AppDbContext _context;

    public OwnerRevenueReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OwnerRevenueReportDto> GetMonthlyReportAsync(
        int ownerId,
        int year,
        int month,
        int? courtId = null)
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
        var bookings = _context.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Court!.OwnerId == ownerId
                && booking.BookingDate >= monthStart
                && booking.BookingDate < nextMonthStart);

        if (courtId.HasValue)
        {
            bookings = bookings.Where(booking => booking.CourtId == courtId.Value);
        }

        var summary = await bookings
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalBookings = group.Count(),
                ConfirmedBookings = group.Count(booking => booking.Status == BookingStatus.Confirmed),
                CompletedBookings = group.Count(booking => booking.Status == BookingStatus.Completed),
                PendingBookings = group.Count(booking => booking.Status == BookingStatus.Pending),
                CancelledBookings = group.Count(booking => booking.Status == BookingStatus.Cancelled),
                ExpiredBookings = group.Count(booking => booking.Status == BookingStatus.Expired),
                BookingValue = group
                    .Where(booking => booking.Status == BookingStatus.Confirmed
                        || booking.Status == BookingStatus.Completed)
                    .Sum(booking => (decimal?)booking.TotalPrice) ?? 0m,
                CollectedAmount = group
                    .Where(booking => booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Paid
                            || booking.Payment.Status == PaymentStatus.DemoPaid))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m,
                RefundedAmount = group
                    .Where(booking => booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Refunded
                            || booking.Payment.Status == PaymentStatus.DemoRefunded))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m,
                CollectedForCancelledBookings = group
                    .Where(booking => (booking.Status == BookingStatus.Cancelled
                            || booking.Status == BookingStatus.Expired)
                        && booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Paid
                            || booking.Payment.Status == PaymentStatus.DemoPaid))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m
            })
            .SingleOrDefaultAsync();

        var courtReports = await bookings
            .GroupBy(booking => new { booking.CourtId, booking.Court!.Name })
            .Select(group => new OwnerCourtRevenueDto
            {
                CourtId = group.Key.CourtId,
                CourtName = group.Key.Name,
                TotalBookings = group.Count(),
                SuccessfulBookings = group.Count(booking =>
                    booking.Status == BookingStatus.Confirmed
                    || booking.Status == BookingStatus.Completed),
                BookingValue = group
                    .Where(booking => booking.Status == BookingStatus.Confirmed
                        || booking.Status == BookingStatus.Completed)
                    .Sum(booking => (decimal?)booking.TotalPrice) ?? 0m,
                CollectedAmount = group
                    .Where(booking => booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Paid
                            || booking.Payment.Status == PaymentStatus.DemoPaid))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m,
                RefundedAmount = group
                    .Where(booking => booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Refunded
                            || booking.Payment.Status == PaymentStatus.DemoRefunded))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m,
                CollectedForCancelledBookings = group
                    .Where(booking => (booking.Status == BookingStatus.Cancelled
                            || booking.Status == BookingStatus.Expired)
                        && booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Paid
                            || booking.Payment.Status == PaymentStatus.DemoPaid))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m
            })
            .OrderByDescending(report => report.BookingValue)
            .ThenBy(report => report.CourtName)
            .ToListAsync();

        var dailyReports = await bookings
            .GroupBy(booking => booking.BookingDate)
            .Select(group => new OwnerDailyRevenueDto
            {
                Date = group.Key,
                TotalBookings = group.Count(),
                SuccessfulBookings = group.Count(booking =>
                    booking.Status == BookingStatus.Confirmed
                    || booking.Status == BookingStatus.Completed),
                BookingValue = group
                    .Where(booking => booking.Status == BookingStatus.Confirmed
                        || booking.Status == BookingStatus.Completed)
                    .Sum(booking => (decimal?)booking.TotalPrice) ?? 0m,
                CollectedAmount = group
                    .Where(booking => booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Paid
                            || booking.Payment.Status == PaymentStatus.DemoPaid))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m,
                RefundedAmount = group
                    .Where(booking => booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Refunded
                            || booking.Payment.Status == PaymentStatus.DemoRefunded))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m,
                CollectedForCancelledBookings = group
                    .Where(booking => (booking.Status == BookingStatus.Cancelled
                            || booking.Status == BookingStatus.Expired)
                        && booking.Payment != null
                        && (booking.Payment.Status == PaymentStatus.Paid
                            || booking.Payment.Status == PaymentStatus.DemoPaid))
                    .Sum(booking => (decimal?)booking.Payment!.Amount) ?? 0m
            })
            .ToDictionaryAsync(report => report.Date);

        var report = new OwnerRevenueReportDto
        {
            MonthStart = monthStart,
            TotalBookings = summary?.TotalBookings ?? 0,
            ConfirmedBookings = summary?.ConfirmedBookings ?? 0,
            CompletedBookings = summary?.CompletedBookings ?? 0,
            PendingBookings = summary?.PendingBookings ?? 0,
            CancelledBookings = summary?.CancelledBookings ?? 0,
            ExpiredBookings = summary?.ExpiredBookings ?? 0,
            BookingValue = summary?.BookingValue ?? 0m,
            CollectedAmount = summary?.CollectedAmount ?? 0m,
            RefundedAmount = summary?.RefundedAmount ?? 0m,
            CollectedForCancelledBookings = summary?.CollectedForCancelledBookings ?? 0m,
            CourtReports = courtReports
        };

        for (var date = monthStart; date < nextMonthStart; date = date.AddDays(1))
        {
            if (dailyReports.TryGetValue(date, out var dailyReport))
            {
                report.DailyReports.Add(dailyReport);
            }
            else
            {
                report.DailyReports.Add(new OwnerDailyRevenueDto { Date = date });
            }
        }

        return report;
    }
}
