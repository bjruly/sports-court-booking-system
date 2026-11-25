using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class CourtOwnerSchemaExtensions
{
    private const string AddOwnerColumnSql = """
        ALTER TABLE "Courts"
            ADD COLUMN IF NOT EXISTS "OwnerId" integer NULL;
        """;

    private const string CompleteOwnerSchemaSql = """
        ALTER TABLE "Courts"
            ALTER COLUMN "OwnerId" SET NOT NULL;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'FK_Courts_Users_OwnerId'
                  AND conrelid = '"Courts"'::regclass
            ) THEN
                ALTER TABLE "Courts"
                    ADD CONSTRAINT "FK_Courts_Users_OwnerId"
                    FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
            END IF;
        END $$;

        CREATE INDEX IF NOT EXISTS "IX_Courts_OwnerId" ON "Courts" ("OwnerId");
        """;

    public static async Task EnsureCourtOwnerSchemaAsync(this AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Court ownership schema requires PostgreSQL.");
        }

        await db.Database.ExecuteSqlRawAsync(AddOwnerColumnSql, cancellationToken);

        var demoOwnerId = await db.GetDemoOwnerIdAsync(cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Courts\" SET \"OwnerId\" = {demoOwnerId} WHERE \"OwnerId\" IS NULL",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(CompleteOwnerSchemaSql, cancellationToken);
    }
}
