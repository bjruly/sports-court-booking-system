using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<CourtType> CourtTypes => Set<CourtType>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Cấu hình bảng User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
        });

        // 2. Cấu hình bảng CourtType
        modelBuilder.Entity<CourtType>(entity =>
        {
            entity.HasKey(ct => ct.Id);
            entity.Property(ct => ct.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(facility => facility.Id);
            entity.Property(facility => facility.Name).IsRequired().HasMaxLength(150);
            entity.Property(facility => facility.Address).IsRequired().HasMaxLength(255);
            entity.Property(facility => facility.Description).HasMaxLength(1000);
            entity.HasIndex(facility => facility.OwnerId);

            entity.HasOne(facility => facility.Owner)
                .WithMany(owner => owner.OwnedFacilities)
                .HasForeignKey(facility => facility.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. Cấu hình bảng Court
        modelBuilder.Entity<Court>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Location).IsRequired().HasMaxLength(255);
            entity.Property(c => c.PricePerHour).HasColumnType("decimal(18,2)");
            entity.Property(c => c.PeakSurcharge).HasColumnType("decimal(18,2)");
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_courts_peak_pricing",
                "\"PeakStartHour\" >= 6 AND \"PeakStartHour\" < 22 AND \"PeakSurcharge\" >= 0"));

            entity.HasIndex(c => c.OwnerId);

            entity.HasOne(c => c.Owner)
                  .WithMany(u => u.OwnedCourts)
                  .HasForeignKey(c => c.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(court => court.Facility)
                .WithMany(facility => facility.Courts)
                .HasForeignKey(court => court.FacilityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CourtType)
                  .WithMany(ct => ct.Courts)
                  .HasForeignKey(c => c.CourtTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(voucher => voucher.Id);
            entity.Property(voucher => voucher.Code).IsRequired().HasMaxLength(30);
            entity.Property(voucher => voucher.DiscountValue).HasColumnType("decimal(18,2)");
            entity.HasIndex(voucher => new { voucher.OwnerId, voucher.Code }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "ck_vouchers_values",
                    "(\"DiscountType\" = 1 AND \"DiscountValue\" > 0 AND \"DiscountValue\" <= 100) " +
                    "OR (\"DiscountType\" = 2 AND \"DiscountValue\" > 0)");
                table.HasCheckConstraint(
                    "ck_vouchers_dates_and_usage",
                    "\"EndDate\" >= \"StartDate\" AND (\"UsageLimit\" IS NULL OR \"UsageLimit\" > 0)");
            });

            entity.HasOne(voucher => voucher.Owner)
                .WithMany(owner => owner.OwnedVouchers)
                .HasForeignKey(voucher => voucher.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. Cấu hình bảng Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(b => b.VoucherCode).HasMaxLength(30);
            entity.Property(b => b.GuestName).HasMaxLength(100);
            entity.Property(b => b.GuestPhone).HasMaxLength(20);
            entity.Property(b => b.CheckInAt).HasColumnType("timestamp with time zone");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "ck_bookings_customer_identity",
                    "\"UserId\" IS NOT NULL OR (\"GuestName\" IS NOT NULL AND \"GuestPhone\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_bookings_voucher_discount",
                    "\"DiscountAmount\" >= 0 AND " +
                    "((\"VoucherId\" IS NULL AND \"VoucherCode\" IS NULL AND \"DiscountAmount\" = 0) " +
                    "OR (\"VoucherId\" IS NOT NULL AND \"VoucherCode\" IS NOT NULL))");
            });

            entity.HasOne(b => b.User)
                  .WithMany(u => u.Bookings)
                  .HasForeignKey(b => b.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Court)
                  .WithMany(c => c.Bookings)
                  .HasForeignKey(b => b.CourtId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Voucher)
                  .WithMany(voucher => voucher.Bookings)
                  .HasForeignKey(b => b.VoucherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 5. Cấu hình bảng Payment (Quan hệ 1 - 1 với Booking)
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.Property(p => p.PaymentMethod).HasMaxLength(50);

            entity.HasOne(p => p.Booking)
                  .WithOne(b => b.Payment)
                  .HasForeignKey<Payment>(p => p.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. Cấu hình bảng Review
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Comment).HasMaxLength(500);
            entity.Property(r => r.OwnerReply).HasMaxLength(1000);
            entity.Property(r => r.OwnerRepliedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(r => r.BookingId).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_reviews_rating", "\"Rating\" >= 1 AND \"Rating\" <= 5");
                table.HasCheckConstraint(
                    "ck_reviews_owner_reply",
                    "(\"OwnerReply\" IS NULL AND \"OwnerRepliedAt\" IS NULL) " +
                    "OR (\"OwnerReply\" IS NOT NULL AND \"OwnerRepliedAt\" IS NOT NULL)");
            });

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Reviews)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Court)
                  .WithMany(c => c.Reviews)
                  .HasForeignKey(r => r.CourtId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Booking)
                  .WithOne(b => b.Review)
                  .HasForeignKey<Review>(r => r.BookingId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. Khởi tạo dữ liệu mẫu (Seed Data)
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed CourtTypes
        modelBuilder.Entity<CourtType>().HasData(
            new CourtType { Id = 1, Name = "Cầu lông", Description = "Sân cầu lông thảm tiêu chuẩn thi đấu", CreatedAt = seedDate },
            new CourtType { Id = 2, Name = "Bóng đá mini", Description = "Sân cỏ nhân tạo 5 người và 7 người", CreatedAt = seedDate },
            new CourtType { Id = 3, Name = "Pickleball", Description = "Sân Pickleball có mái che thoáng mát", CreatedAt = seedDate },
            new CourtType { Id = 4, Name = "Tennis", Description = "Sân cứng tiêu chuẩn quốc tế", CreatedAt = seedDate }
        );

        // Seed Courts
        modelBuilder.Entity<Court>().HasData(
            new Court { Id = 1, OwnerId = 3, Name = "Sân Cầu Lông VIP 1", CourtTypeId = 1, Location = "Khu A - Tầng 1", PricePerHour = 80000, Status = CourtStatus.Active, Description = "Thảm Yonex, đèn LED chống chói", CreatedAt = seedDate },
            new Court { Id = 2, OwnerId = 3, Name = "Sân Cầu Lông VIP 2", CourtTypeId = 1, Location = "Khu A - Tầng 1", PricePerHour = 80000, Status = CourtStatus.Active, Description = "Thảm Yonex, quạt làm mát", CreatedAt = seedDate },
            new Court { Id = 3, OwnerId = 3, Name = "Sân Bóng Đá 5 Người - Sân A", CourtTypeId = 2, Location = "Khu B - Ngoài trời", PricePerHour = 250000, Status = CourtStatus.Active, Description = "Cỏ mới thay, hệ thống thoát nước tốt", CreatedAt = seedDate },
            new Court { Id = 4, OwnerId = 3, Name = "Sân Pickleball Số 1", CourtTypeId = 3, Location = "Khu C - Có mái che", PricePerHour = 120000, Status = CourtStatus.Active, Description = "Mặt sân Acrylic chuẩn thi đấu", CreatedAt = seedDate }
        );

        // Seed Users (Admin, Chủ Sân & Khách hàng mẫu)
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, FullName = "Quản Trị Viên", Email = "admin@sportscourt.vn", PhoneNumber = "0901234567", PasswordHash = SeedCredentials.AdminPasswordHash, Role = UserRole.Admin, IsActive = true, CreatedAt = seedDate },
            new User { Id = 2, FullName = "Nguyễn Văn Khách", Email = "khachhang@gmail.com", PhoneNumber = "0987654321", PasswordHash = SeedCredentials.CustomerPasswordHash, Role = UserRole.Customer, IsActive = true, CreatedAt = seedDate },
            new User { Id = 3, FullName = "Chủ Sân Demo", Email = SeedCredentials.OwnerEmail, PhoneNumber = "0901111111", PasswordHash = SeedCredentials.OwnerPasswordHash, Role = UserRole.CourtOwner, IsActive = true, CreatedAt = seedDate }
        );
    }
}