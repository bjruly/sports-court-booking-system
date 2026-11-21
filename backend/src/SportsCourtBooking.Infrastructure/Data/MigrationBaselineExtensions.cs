using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class MigrationBaselineExtensions
{
    private const string CountExistingTablesSql = """
        SELECT COUNT(*)::integer AS "Value"
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_type = 'BASE TABLE'
          AND table_name IN (
              'Users',
              'CourtTypes',
              'Courts',
              'Facilities',
              'Bookings',
              'Payments',
              'Reviews',
              'Vouchers'
          )
        """;

    private const string CreateHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
            "MigrationId" character varying(150) NOT NULL,
            "ProductVersion" character varying(32) NOT NULL,
            CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
        );
        """;

    public static async Task BaselineExistingDatabaseAsync(
        this AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Migration baselining requires PostgreSQL.");
        }

        var initialMigration = db.Database.GetMigrations().FirstOrDefault();
        if (initialMigration is null)
        {
            return;
        }

        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (appliedMigrations.Contains(initialMigration, StringComparer.Ordinal))
        {
            return;
        }

        var existingTableCount = await db.Database
            .SqlQueryRaw<int>(CountExistingTablesSql)
            .SingleAsync(cancellationToken);

        if (existingTableCount == 0)
        {
            return;
        }

        if (existingTableCount != 8)
        {
            throw new InvalidOperationException(
                "Existing database has an incomplete schema. Inspect and back up the database before applying migrations.");
        }

        await db.Database.ExecuteSqlRawAsync(CreateHistoryTableSql, cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({initialMigration}, {"8.0.11"})
            ON CONFLICT ("MigrationId") DO NOTHING;
            """,
            cancellationToken);
    }
}
