using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class CourtDistrictSchemaExtensions
{
    private const string CourtDistrictSchemaSql = """
        ALTER TABLE "Courts"
            ADD COLUMN IF NOT EXISTS "District" character varying(100) NOT NULL DEFAULT 'Quận 10';

        -- 1. CẦU LÔNG - QUẬN 7 (Sân 1, 2, 3)
        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (1, {0}, 'Sân Cầu Lông VIP 1', 1, 'Quận 7, TP. Hồ Chí Minh', 'Quận 7', 80000, 1, 'Thảm Yonex đạt chuẩn BWF thi đấu, đèn LED chống chói mắt.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (6, {0}, 'Sân Cầu Lông VIP 2', 1, 'Quận 7, TP. Hồ Chí Minh', 'Quận 7', 80000, 1, 'Thảm Yonex chống trơn trượt, hệ thống quạt mát tự nhiên.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (7, {0}, 'Sân Cầu Lông VIP 3', 1, 'Quận 7, TP. Hồ Chí Minh', 'Quận 7', 90000, 1, 'Sân VIP trung tâm, có ghế nghỉ vận động viên và khán đài mini.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        -- 2. CẦU LÔNG - BÌNH THẠNH (Sân 1, 2, 3)
        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (2, {0}, 'Sân Cầu Lông 1 - Bình Thạnh', 1, 'Bình Thạnh, TP. Hồ Chí Minh', 'Bình Thạnh', 75000, 1, 'Thảm cao su chống sốc, không gian thoáng mát.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (8, {0}, 'Sân Cầu Lông 2 - Bình Thạnh', 1, 'Bình Thạnh, TP. Hồ Chí Minh', 'Bình Thạnh', 75000, 1, 'Thảm thi đấu tiêu chuẩn, hệ thống đèn chiếu sáng góc rộng.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (9, {0}, 'Sân Cầu Lông 3 - Bình Thạnh', 1, 'Bình Thạnh, TP. Hồ Chí Minh', 'Bình Thạnh', 85000, 1, 'Sân VIP máy lạnh, sàn cao cấp nhập khẩu.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        -- 3. BÓNG ĐÁ - THỦ ĐỨC (Sân 1, 2, 3)
        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (3, {0}, 'Sân Bóng Đá 5 Người (Sân A)', 2, 'Thủ Đức, TP. Hồ Chí Minh', 'Thủ Đức', 250000, 1, 'Mặt cỏ nhân tạo mới thay, hệ thống thoát nước mưa cực tốt.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (10, {0}, 'Sân Bóng Đá 5 Người (Sân B)', 2, 'Thủ Đức, TP. Hồ Chí Minh', 'Thủ Đức', 250000, 1, 'Cỏ sợi mềm tiêu chuẩn FIFA, đèn cao áp chiếu sáng đêm.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (11, {0}, 'Sân Bóng Đá 7 Người (Sân C)', 2, 'Thủ Đức, TP. Hồ Chí Minh', 'Thủ Đức', 350000, 1, 'Sân thi đấu 7 người rộng rãi, khán đài có mái che.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        -- 4. PICKLEBALL - QUẬN 10 (Sân 1, 2, 3)
        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (4, {0}, 'Sân Pickleball Số 1', 3, 'Quận 10, TP. Hồ Chí Minh', 'Quận 10', 120000, 1, 'Mặt sân Acrylic tiêu chuẩn thi đấu, chống trơn trượt tối đa.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (12, {0}, 'Sân Pickleball Số 2', 3, 'Quận 10, TP. Hồ Chí Minh', 'Quận 10', 120000, 1, 'Mặt sân cao cấp, có mái che mát mẻ và hệ thống thông gió.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (13, {0}, 'Sân Pickleball Số 3', 3, 'Quận 10, TP. Hồ Chí Minh', 'Quận 10', 130000, 1, 'Sân VIP trung tâm có lưới thi đấu chuyên nghiệp USA Pickleball.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        -- 5. TENNIS - QUẬN 2 (Sân 1, 2, 3)
        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (5, {0}, 'Sân Tennis Tiêu Chuẩn 1', 4, 'Quận 2, TP. Hồ Chí Minh', 'Quận 2', 180000, 1, 'Mặt sân cứng Plexicushion tiêu chuẩn Grand Slam.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (14, {0}, 'Sân Tennis Tiêu Chuẩn 2', 4, 'Quận 2, TP. Hồ Chí Minh', 'Quận 2', 180000, 1, 'Mặt sân cứng cao cấp, đèn LED công suất lớn không chói.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        INSERT INTO "Courts" ("Id", "OwnerId", "Name", "CourtTypeId", "Location", "District", "PricePerHour", "Status", "Description", "CreatedAt", "PeakStartHour", "PeakSurcharge")
        VALUES (15, {0}, 'Sân Tennis VIP Có Mái Che', 4, 'Quận 2, TP. Hồ Chí Minh', 'Quận 2', 220000, 1, 'Sân có mái che hiện đại, đánh thoải mái bất kể thời tiết.', '2026-01-01 00:00:00+00', 17, 50000)
        ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "CourtTypeId" = EXCLUDED."CourtTypeId", "Location" = EXCLUDED."Location", "District" = EXCLUDED."District", "PricePerHour" = EXCLUDED."PricePerHour", "Description" = EXCLUDED."Description";

        ALTER TABLE "Courts"
            ALTER COLUMN "District" DROP DEFAULT;
        """;

    public static async Task EnsureCourtDistrictSchemaAsync(
        this AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Court district schema requires PostgreSQL.");
        }

        var demoOwnerId = await db.GetDemoOwnerIdAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(CourtDistrictSchemaSql, new object[] { demoOwnerId }, cancellationToken);
    }
}
