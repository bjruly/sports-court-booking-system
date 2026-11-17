using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class CourtPricingSchemaExtensions
{
    private const string CourtPricingSchemaSql = """
        ALTER TABLE "Courts"
            ADD COLUMN IF NOT EXISTS "PeakStartHour" integer NOT NULL DEFAULT 17;

        ALTER TABLE "Courts"
            ADD COLUMN IF NOT EXISTS "PeakSurcharge" numeric(18,2) NOT NULL DEFAULT 50000;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'ck_courts_peak_pricing'
                  AND conrelid = '"Courts"'::regclass
            ) THEN
                ALTER TABLE "Courts"
                    ADD CONSTRAINT ck_courts_peak_pricing
                    CHECK ("PeakStartHour" >= 6 AND "PeakStartHour" < 22 AND "PeakSurcharge" >= 0);
            END IF;
        END $$;
        """;

    public static Task EnsureCourtPricingSchemaAsync(
        this AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Court pricing schema requires PostgreSQL.");
        }

        return db.Database.ExecuteSqlRawAsync(CourtPricingSchemaSql, cancellationToken);
    }
}
