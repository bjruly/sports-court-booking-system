using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class BookingSchemaExtensions
{
    private const string BookingSchemaSql = """
        CREATE EXTENSION IF NOT EXISTS btree_gist;

        ALTER TABLE "Bookings"
            ADD COLUMN IF NOT EXISTS "ExpiresAt" timestamp with time zone NULL;

        UPDATE "Bookings"
        SET "ExpiresAt" = "CreatedAt" + interval '10 minutes'
        WHERE "Status" = 1 AND "ExpiresAt" IS NULL;

        UPDATE "Bookings"
        SET "Status" = 5,
            "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Status" = 1 AND "ExpiresAt" <= CURRENT_TIMESTAMP;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'ck_bookings_pending_expires_at'
                  AND conrelid = '"Bookings"'::regclass
            ) THEN
                ALTER TABLE "Bookings"
                ADD CONSTRAINT ck_bookings_pending_expires_at
                CHECK ("Status" <> 1 OR "ExpiresAt" IS NOT NULL);
            END IF;
        END $$;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'ex_booking_reservations_no_overlap'
                  AND conrelid = '"Bookings"'::regclass
            ) THEN
                ALTER TABLE "Bookings"
                ADD CONSTRAINT ex_booking_reservations_no_overlap
                EXCLUDE USING gist (
                    "CourtId" WITH =,
                    tsrange("BookingDate" + "StartTime", "BookingDate" + "EndTime", '[)') WITH &&
                )
                WHERE ("Status" IN (1, 2));
            END IF;
        END $$;
        """;

    public static Task EnsureBookingSchemaAsync(this AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Booking exclusion constraints require PostgreSQL.");
        }

        return db.Database.ExecuteSqlRawAsync(BookingSchemaSql, cancellationToken);
    }
}
