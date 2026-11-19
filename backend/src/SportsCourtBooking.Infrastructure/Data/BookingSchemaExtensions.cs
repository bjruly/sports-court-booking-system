using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;

namespace SportsCourtBooking.Infrastructure.Data;

public static class BookingSchemaExtensions
{
    private const string BookingSchemaSql = """
        CREATE EXTENSION IF NOT EXISTS btree_gist;

        ALTER TABLE "Bookings"
            ADD COLUMN IF NOT EXISTS "ExpiresAt" timestamp with time zone NULL;

        ALTER TABLE "Bookings"
            ADD COLUMN IF NOT EXISTS "GuestName" character varying(100) NULL;

        ALTER TABLE "Bookings"
            ADD COLUMN IF NOT EXISTS "GuestPhone" character varying(20) NULL;

        ALTER TABLE "Bookings"
            ADD COLUMN IF NOT EXISTS "CheckInAt" timestamp with time zone NULL;

        ALTER TABLE "Bookings"
            ALTER COLUMN "UserId" DROP NOT NULL;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'ck_bookings_customer_identity'
                  AND conrelid = '"Bookings"'::regclass
            ) THEN
                ALTER TABLE "Bookings"
                ADD CONSTRAINT ck_bookings_customer_identity
                CHECK ("UserId" IS NOT NULL OR ("GuestName" IS NOT NULL AND "GuestPhone" IS NOT NULL));
            END IF;
        END $$;

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

        -- UC13, UC23: Cấu trúc bảng Facilities và liên kết Courts
        CREATE TABLE IF NOT EXISTS "Facilities" (
            "Id" serial PRIMARY KEY,
            "Name" character varying(150) NOT NULL,
            "Address" character varying(255) NOT NULL,
            "PhoneNumber" character varying(30) NOT NULL,
            "Description" text NULL,
            "OpenTime" character varying(10) NOT NULL DEFAULT '06:00',
            "CloseTime" character varying(10) NOT NULL DEFAULT '22:00',
            "ImageUrl" character varying(500) NULL,
            "OwnerId" integer NULL REFERENCES "Users"("Id") ON DELETE SET NULL,
            "IsApproved" boolean NOT NULL DEFAULT true,
            "IsActive" boolean NOT NULL DEFAULT true,
            "SuspensionReason" character varying(500) NULL,
            "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "UpdatedAt" timestamp with time zone NULL
        );

        ALTER TABLE "Courts"
            ADD COLUMN IF NOT EXISTS "FacilityId" integer NULL REFERENCES "Facilities"("Id") ON DELETE SET NULL;
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

        // Seed dữ liệu mẫu cho Facilities nếu chưa có để phục vụ demo UC13, UC23
        if (!await db.Facilities.AnyAsync(cancellationToken))
        {
            var seedTime = DateTime.UtcNow.AddMonths(-1);
            var sampleFacilities = new List<Facility>
            {
                new()
                {
                    Name = "CLB Thể Thao SGU - Cơ sở An Dương Vương",
                    Address = "105 An Dương Vương, Phường 4, Quận 5, TP.HCM",
                    PhoneNumber = "028 3835 4409",
                    Description = "Cụm sân thể thao trong nhà trung tâm TP.HCM, trang bị sàn gỗ thảm Yonex và hệ thống chiếu sáng chuẩn quốc tế.",
                    OpenTime = "06:00",
                    CloseTime = "22:30",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = seedTime
                },
                new()
                {
                    Name = "Khu Liên Hợp Thể Thao Kỳ Hòa",
                    Address = "238 Ba Tháng Hai, Phường 12, Quận 10, TP.HCM",
                    PhoneNumber = "090 987 6543",
                    Description = "Tổ hợp sân bóng đá mini cỏ nhân tạo và cụm sân Pickleball ngoài trời thoáng đãng, bãi xe ô tô rộng rãi.",
                    OpenTime = "05:30",
                    CloseTime = "23:00",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = seedTime.AddDays(5)
                },
                new()
                {
                    Name = "Cụm Sân Thể Thao Rạch Miễu",
                    Address = "Số 1 Hoa Phượng, Phường 2, Quận Phú Nhuận, TP.HCM",
                    PhoneNumber = "091 234 5678",
                    Description = "Cơ sở mới đăng ký tham gia nền tảng Sân Nhà. Đang nộp hồ sơ giấy phép kinh doanh chờ phê duyệt.",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    IsApproved = false, // Chờ phê duyệt UC23
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-18)
                },
                new()
                {
                    Name = "Sân Thể Thao Bình Thạnh VIP",
                    Address = "12 Chu Văn An, Phường 12, Quận Bình Thạnh, TP.HCM",
                    PhoneNumber = "093 333 4455",
                    Description = "Cơ sở có 3 sân bóng đá mini và 2 sân cầu lông.",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    IsApproved = true,
                    IsActive = false, // Tạm khóa vi phạm UC23
                    SuspensionReason = "Cơ sở chưa hoàn tất nghiệm thu an toàn PCCC và nhận phản ánh chất lượng từ nhiều khách hàng.",
                    CreatedAt = seedTime.AddDays(10)
                }
            };

            await db.Facilities.AddRangeAsync(sampleFacilities, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            // Gán các sân hiện có vào cơ sở 1 và cơ sở 2
            var courts = await db.Courts.ToListAsync(cancellationToken);
            if (courts.Count >= 4)
            {
                courts[0].FacilityId = sampleFacilities[0].Id;
                courts[1].FacilityId = sampleFacilities[0].Id;
                courts[2].FacilityId = sampleFacilities[1].Id;
                courts[3].FacilityId = sampleFacilities[1].Id;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
