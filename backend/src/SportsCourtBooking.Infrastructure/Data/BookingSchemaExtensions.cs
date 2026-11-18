using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;

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

        -- UC26: Cấu trúc bảng Reviews
        ALTER TABLE "Reviews"
            ADD COLUMN IF NOT EXISTS "IsHidden" boolean NOT NULL DEFAULT false,
            ADD COLUMN IF NOT EXISTS "HiddenReason" character varying(500) NULL,
            ADD COLUMN IF NOT EXISTS "HiddenAt" timestamp with time zone NULL,
            ADD COLUMN IF NOT EXISTS "IsReported" boolean NOT NULL DEFAULT false;
        """;

    public static async Task EnsureBookingSchemaAsync(this AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("Booking exclusion constraints require PostgreSQL.");
        }

        await db.Database.ExecuteSqlRawAsync(BookingSchemaSql, cancellationToken);

        // Seed dữ liệu mẫu cho Reviews nếu chưa có để phục vụ demo UC26
        if (!await db.Reviews.AnyAsync(cancellationToken))
        {
            var seedTime = DateTime.UtcNow.AddDays(-2);
            var sampleReviews = new List<Review>
            {
                new()
                {
                    UserId = 2,
                    CourtId = 1,
                    Rating = 5,
                    Comment = "Sân Cầu Lông VIP 1 rất đẹp, thảm Yonex mới và hệ thống đèn LED chống chói chuẩn thi đấu. Sẽ quay lại thường xuyên!",
                    IsHidden = false,
                    IsReported = false,
                    CreatedAt = seedTime
                },
                new()
                {
                    UserId = 2,
                    CourtId = 3,
                    Rating = 4,
                    Comment = "Sân bóng đá cỏ nhân tạo mới thay, thoát nước tốt sau mưa. Bãi giữ xe rộng rãi.",
                    IsHidden = false,
                    IsReported = false,
                    CreatedAt = seedTime.AddHours(5)
                },
                new()
                {
                    UserId = 2,
                    CourtId = 2,
                    Rating = 1,
                    Comment = "Sân phục vụ như lừa đảo, nhân viên thái độ mất dạy coi thường khách, sân bẩn như bãi rác đừng ai vào đây thuê!!!",
                    IsHidden = false,
                    IsReported = true, // Đang có báo cáo vi phạm
                    CreatedAt = seedTime.AddHours(10)
                },
                new()
                {
                    UserId = 2,
                    CourtId = 4,
                    Rating = 1,
                    Comment = "CHO VAY TIỀN NHANH TRONG NGÀY KHÔNG THẾ CHẤP LIÊN HỆ ZALO 0988XXXXXX LÃI SUẤT 0% GIẢI NGÂN SAU 10 PHÚT",
                    IsHidden = true,
                    HiddenReason = "Spam / Quảng cáo dịch vụ tài chính trái phép không liên quan đến sân",
                    HiddenAt = DateTime.UtcNow.AddDays(-1),
                    IsReported = true,
                    CreatedAt = seedTime.AddHours(14)
                }
            };

            await db.Reviews.AddRangeAsync(sampleReviews, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
