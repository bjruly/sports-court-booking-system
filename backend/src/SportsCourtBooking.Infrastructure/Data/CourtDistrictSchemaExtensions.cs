using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class CourtDistrictSchemaExtensions
{
    private const string CourtDistrictSchemaSql = """
        ALTER TABLE "Courts"
            ADD COLUMN IF NOT EXISTS "District" character varying(100) NOT NULL DEFAULT 'Quận 10';

        UPDATE "Courts"
        SET "District" = 'Quận 7'
        WHERE "Id" = 1 AND ("District" IS NULL OR "District" = '' OR "District" = 'Quận 10');

        UPDATE "Courts"
        SET "District" = 'Bình Thạnh'
        WHERE "Id" = 2 AND ("District" IS NULL OR "District" = '' OR "District" = 'Quận 10');

        UPDATE "Courts"
        SET "District" = 'Thủ Đức'
        WHERE "Id" = 3 AND ("District" IS NULL OR "District" = '' OR "District" = 'Quận 10');

        ALTER TABLE "Courts"
            ALTER COLUMN "District" DROP DEFAULT;
        """;

    public static Task EnsureCourtDistrictSchemaAsync(
        this AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Court district schema requires PostgreSQL.");
        }

        return db.Database.ExecuteSqlRawAsync(CourtDistrictSchemaSql, cancellationToken);
    }
}
