using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsCourtBooking.Infrastructure.Data.Migrations;

public sealed partial class AddDemoFacilities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "Facilities"
                ("Id", "OwnerId", "Name", "Address", "PhoneNumber", "Description", "OpenTime", "CloseTime", "IsApproved", "IsActive", "CreatedAt")
            VALUES
                (10001, 3, 'CLB Cầu Lông VIP Quận 7', 'Quận 7, TP. Hồ Chí Minh', '0901111111', 'Cụm sân cầu lông chất lượng cao tại Quận 7.', '06:00', '22:00', TRUE, TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00'),
                (10002, 3, 'Cầu Lông Bình Thạnh', 'Bình Thạnh, TP. Hồ Chí Minh', '0901111111', 'Cụm sân cầu lông tại Bình Thạnh.', '06:00', '22:00', TRUE, TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00'),
                (10003, 3, 'Sân Bóng Đá Thủ Đức', 'Thủ Đức, TP. Hồ Chí Minh', '0901111111', 'Cụm sân bóng đá mini tại Thủ Đức.', '06:00', '22:00', TRUE, TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00'),
                (10004, 3, 'Pickleball Quận 10', 'Quận 10, TP. Hồ Chí Minh', '0901111111', 'Cụm sân pickleball tại Quận 10.', '06:00', '22:00', TRUE, TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00'),
                (10005, 3, 'Tennis Quận 2', 'Quận 2, TP. Hồ Chí Minh', '0901111111', 'Cụm sân tennis tại Quận 2.', '06:00', '22:00', TRUE, TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00')
            ON CONFLICT ("Id") DO NOTHING;

            UPDATE "Courts"
            SET "FacilityId" = CASE
                WHEN "Id" IN (1, 6, 7) THEN 10001
                WHEN "Id" IN (2, 8, 9) THEN 10002
                WHEN "Id" IN (3, 10, 11) THEN 10003
                WHEN "Id" IN (4, 12, 13) THEN 10004
                WHEN "Id" IN (5, 14, 15) THEN 10005
                ELSE "FacilityId"
            END
            WHERE "Id" BETWEEN 1 AND 15 AND "OwnerId" = 3;

            SELECT setval(
                pg_get_serial_sequence('"Facilities"', 'Id'),
                GREATEST(COALESCE((SELECT MAX("Id") FROM "Facilities"), 1), 1),
                TRUE);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Courts" SET "FacilityId" = NULL
            WHERE "FacilityId" BETWEEN 10001 AND 10005 AND "OwnerId" = 3;

            DELETE FROM "Facilities" WHERE "Id" BETWEEN 10001 AND 10005;
            """);
    }
}
