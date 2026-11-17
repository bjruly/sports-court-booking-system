using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public sealed class PendingBookingExpirationService
{
    private readonly AppDbContext _context;

    public PendingBookingExpirationService(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (!_context.Database.IsRelational())
        {
            return ExpireOverdueInMemoryAsync(now, cancellationToken);
        }

        return _context.Bookings
            .Where(booking =>
                booking.Status == BookingStatus.Pending &&
                booking.ExpiresAt.HasValue &&
                booking.ExpiresAt.Value <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(booking => booking.Status, BookingStatus.Expired)
                    .SetProperty(booking => booking.UpdatedAt, now),
                cancellationToken);
    }

    private async Task<int> ExpireOverdueInMemoryAsync(DateTime now, CancellationToken cancellationToken)
    {
        var overdueBookings = await _context.Bookings
            .Where(booking =>
                booking.Status == BookingStatus.Pending &&
                booking.ExpiresAt.HasValue &&
                booking.ExpiresAt.Value <= now)
            .ToListAsync(cancellationToken);

        foreach (var booking in overdueBookings)
        {
            booking.Status = BookingStatus.Expired;
            booking.UpdatedAt = now;
        }

        if (overdueBookings.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return overdueBookings.Count;
    }
}
