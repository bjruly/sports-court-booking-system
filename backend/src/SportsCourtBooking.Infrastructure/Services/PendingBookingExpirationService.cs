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
}
