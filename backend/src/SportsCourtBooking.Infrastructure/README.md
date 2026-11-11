# Infrastructure

Chứa các triển khai kỹ thuật như truy cập PostgreSQL qua Entity Framework Core/Npgsql, gửi email/SMS và tích hợp cổng thanh toán hoặc dịch vụ bản đồ.


# SportsCourtBooking.Infrastructure

## 1. Giới thiệu chung

Đây là tầng **Infrastructure** của hệ thống Đặt sân Thể thao (`SportsCourtBooking`). Tầng này chịu trách nhiệm giao tiếp với Cơ sở dữ liệu **SQL Server** thông qua **Entity Framework Core 8.0**, quản lý cấu hình bảng (Fluent API), lịch sử thay đổi CSDL (Migrations) và nạp dữ liệu mẫu ban đầu (Seed Data).

## 2. Cấu trúc thư mục hiện tại

```text
SportsCourtBooking.Infrastructure/
├── Data/
│   ├── Migrations/                # Chứa các file Migration tự động sinh bảng và Seed Data
│   ├── AppDbContext.cs            # Khai báo DbSet, cấu hình khóa ngoại/ràng buộc và Seed Data
│   └── AppDbContextFactory.cs     # Hỗ trợ công cụ dotnet-ef khởi tạo DbContext lúc chạy lệnh CLI
├── README.md                      # Tài liệu hướng dẫn tầng Infrastructure
└── SportsCourtBooking.Infrastructure.csproj
```
