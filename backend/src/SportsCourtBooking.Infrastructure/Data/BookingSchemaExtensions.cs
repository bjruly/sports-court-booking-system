using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;

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

        // Không tự xóa booking, payment, refund hoặc tài khoản khi ứng dụng khởi động.
        // Booking có thể có Payment/Refund liên quan và dữ liệu này cần được giữ lại.
        var demoOwnerId = await db.GetDemoOwnerIdAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Facilities\" SET \"OwnerId\" = {demoOwnerId} WHERE \"OwnerId\" IS NULL",
            cancellationToken);

        // 1. Đảm bảo đúng 5 Cơ sở thể thao thật khớp 100% với 5 cụm sân trên web
        var facilities = await db.Facilities.ToListAsync(cancellationToken);
        foreach (var facility in facilities.Where(facility => facility.OwnerId is null))
        {
            facility.OwnerId = demoOwnerId;
        }

        if (!facilities.Any(f => f.Name.Contains("Quận 7")))
        {
            var seedTime = DateTime.UtcNow.AddMonths(-1);
            var realFacilities = new List<Facility>
            {
                new()
                {
                    OwnerId = demoOwnerId,
                    Name = "Cơ sở Cầu Lông Nam Sài Gòn - Quận 7",
                    Address = "Quận 7, TP. Hồ Chí Minh",
                    PhoneNumber = "0901 111 001",
                    Description = "Cụm 3 sân cầu lông thảm Yonex đạt chuẩn BWF thi đấu, hệ thống đèn LED chống chói.",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = seedTime
                },
                new()
                {
                    OwnerId = demoOwnerId,
                    Name = "Cơ sở Cầu Lông Chu Văn An - Bình Thạnh",
                    Address = "Bình Thạnh, TP. Hồ Chí Minh",
                    PhoneNumber = "0901 111 002",
                    Description = "Cụm 3 sân cầu lông thảm cao su chống sốc, không gian thoáng mát và có khán đài.",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = seedTime.AddDays(2)
                },
                new()
                {
                    OwnerId = demoOwnerId,
                    Name = "Cụm Sân Bóng Đá Mini Thủ Đức",
                    Address = "Thủ Đức, TP. Hồ Chí Minh",
                    PhoneNumber = "0901 111 003",
                    Description = "Tổ hợp sân bóng đá mini cỏ nhân tạo 5 người và 7 người tiêu chuẩn FIFA, thoát nước mưa tốt.",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = seedTime.AddDays(4)
                },
                new()
                {
                    OwnerId = demoOwnerId,
                    Name = "Trung Tâm Pickleball Quận 10",
                    Address = "Quận 10, TP. Hồ Chí Minh",
                    PhoneNumber = "0901 111 004",
                    Description = "Cụm 3 sân Pickleball mặt acrylic cao cấp chuẩn thi đấu, có mái che mát mẻ ban ngày.",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = seedTime.AddDays(6)
                },
                new()
                {
                    OwnerId = demoOwnerId,
                    Name = "CLB Tennis Thảo Điền - Quận 2",
                    Address = "Quận 2, TP. Hồ Chí Minh",
                    PhoneNumber = "0901 111 005",
                    Description = "Cụm sân tennis mặt cứng Plexicushion tiêu chuẩn Grand Slam và sân có mái che.",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = seedTime.AddDays(8)
                }
            };

            await db.Facilities.AddRangeAsync(realFacilities, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            facilities = realFacilities;
        }

        // 2. Gán chính xác 15 sân thể thao vào 5 cơ sở thật tương ứng
        var courts = await db.Courts.ToListAsync(cancellationToken);
        if (courts.Count >= 15 && facilities.Count >= 5)
        {
            // Cơ sở có thể đã được đổi tên; khi đó giữ nguyên sân thay vì làm ứng dụng không khởi động được.
            var facQ7 = facilities.FirstOrDefault(f => f.Name.Contains("Quận 7"));
            var facBT = facilities.FirstOrDefault(f => f.Name.Contains("Bình Thạnh"));
            var facTD = facilities.FirstOrDefault(f => f.Name.Contains("Thủ Đức"));
            var facQ10 = facilities.FirstOrDefault(f => f.Name.Contains("Quận 10"));
            var facQ2 = facilities.FirstOrDefault(f => f.Name.Contains("Quận 2"));

            // Cầu lông Q7: Sân 1, 6, 7
            foreach (var c in courts.Where(c => facQ7 is not null && (c.Id == 1 || c.Id == 6 || c.Id == 7)))
                c.FacilityId = facQ7!.Id;

            // Cầu lông Bình Thạnh: Sân 2, 8, 9
            foreach (var c in courts.Where(c => facBT is not null && (c.Id == 2 || c.Id == 8 || c.Id == 9)))
                c.FacilityId = facBT!.Id;

            // Bóng đá Thủ Đức: Sân 3, 10, 11
            foreach (var c in courts.Where(c => facTD is not null && (c.Id == 3 || c.Id == 10 || c.Id == 11)))
                c.FacilityId = facTD!.Id;

            // Pickleball Q10: Sân 4, 12, 13
            foreach (var c in courts.Where(c => facQ10 is not null && (c.Id == 4 || c.Id == 12 || c.Id == 13)))
                c.FacilityId = facQ10!.Id;

            // Tennis Q2: Sân 5, 14, 15
            foreach (var c in courts.Where(c => facQ2 is not null && (c.Id == 5 || c.Id == 14 || c.Id == 15)))
                c.FacilityId = facQ2!.Id;

            await db.SaveChangesAsync(cancellationToken);
        }

        // Tự động khởi tạo sân cho bất kỳ cơ sở nào đã duyệt nhưng chưa có sân
        // Đồng bộ sequence sau khi CourtDistrictSchemaExtensions chèn sân bằng Id cố định.
        db.SyncIdentitySequences();

        var approvedFacilities = await db.Facilities
            .Where(f => f.IsApproved && f.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var fac in approvedFacilities)
        {
            var courtCount = await db.Courts.CountAsync(c => c.FacilityId == fac.Id, cancellationToken);
            if (courtCount == 0)
            {
                int courtTypeId = 1;
                string courtTypeName = "Cầu lông";
                decimal price = 80000;

                if (fac.Name.Contains("Pickleball", StringComparison.OrdinalIgnoreCase))
                {
                    courtTypeId = 3;
                    courtTypeName = "Pickleball";
                    price = 120000;
                }
                else if (fac.Name.Contains("Bóng đá", StringComparison.OrdinalIgnoreCase) || fac.Name.Contains("Bóng Đá", StringComparison.OrdinalIgnoreCase))
                {
                    courtTypeId = 2;
                    courtTypeName = "Bóng đá";
                    price = 250000;
                }
                else if (fac.Name.Contains("Tennis", StringComparison.OrdinalIgnoreCase))
                {
                    courtTypeId = 4;
                    courtTypeName = "Tennis";
                    price = 180000;
                }

                var district = fac.Address.Contains(',') ? fac.Address.Split(',')[0].Trim() : "Quận 10";

                for (int i = 1; i <= 2; i++)
                {
                    db.Courts.Add(new Court
                    {
                        OwnerId = fac.OwnerId ?? demoOwnerId,
                        FacilityId = fac.Id,
                        CourtTypeId = courtTypeId,
                        Name = $"Sân {i} ({courtTypeName})",
                        Location = fac.Address,
                        District = district,
                        PricePerHour = price,
                        PeakStartHour = 17,
                        PeakSurcharge = 50000,
                        Status = CourtStatus.Active,
                        Description = fac.Description ?? "Sân đạt chuẩn thi đấu, sạch sẽ thoáng mát.",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
