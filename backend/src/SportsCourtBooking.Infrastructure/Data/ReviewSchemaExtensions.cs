using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class ReviewSchemaExtensions
{
    private const string ReviewSchemaSql = """
        ALTER TABLE "Reviews"
            ADD COLUMN IF NOT EXISTS "BookingId" integer NULL;

        ALTER TABLE "Reviews"
            ADD COLUMN IF NOT EXISTS "OwnerReply" character varying(1000) NULL;

        ALTER TABLE "Reviews"
            ADD COLUMN IF NOT EXISTS "OwnerRepliedAt" timestamp with time zone NULL;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'FK_Reviews_Bookings_BookingId'
                  AND conrelid = '"Reviews"'::regclass
            ) THEN
                ALTER TABLE "Reviews"
                    ADD CONSTRAINT "FK_Reviews_Bookings_BookingId"
                    FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE RESTRICT;
            END IF;

            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'ck_reviews_rating'
                  AND conrelid = '"Reviews"'::regclass
            ) THEN
                ALTER TABLE "Reviews"
                    ADD CONSTRAINT ck_reviews_rating
                    CHECK ("Rating" >= 1 AND "Rating" <= 5);
            END IF;

            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'ck_reviews_owner_reply'
                  AND conrelid = '"Reviews"'::regclass
            ) THEN
                ALTER TABLE "Reviews"
                    ADD CONSTRAINT ck_reviews_owner_reply
                    CHECK (("OwnerReply" IS NULL AND "OwnerRepliedAt" IS NULL)
                        OR ("OwnerReply" IS NOT NULL AND "OwnerRepliedAt" IS NOT NULL));
            END IF;
        END $$;

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Reviews_BookingId"
            ON "Reviews" ("BookingId");
        """;

    public static Task EnsureReviewSchemaAsync(
        this AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Review schema requires PostgreSQL.");
        }

        return db.Database.ExecuteSqlRawAsync(ReviewSchemaSql, cancellationToken);
    }
}
