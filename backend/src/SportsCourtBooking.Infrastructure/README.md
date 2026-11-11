# Infrastructure

Chứa các triển khai kỹ thuật như truy cập PostgreSQL qua Entity Framework Core/Npgsql, gửi email/SMS và tích hợp cổng thanh toán hoặc dịch vụ bản đồ.


# SportsCourtBooking.Infrastructure

## 1. Giới thiệu chung & Vai trò trong đồ án

Đây là tầng **Infrastructure (Hạ tầng)** của hệ thống Đặt sân Thể thao (`SportsCourtBooking`).
Tầng này đảm nhận 2 nhiệm vụ quan trọng nhất:

1. **Quản lý Cơ sở dữ liệu (Database):** Biến các class ở tầng `Domain` thành bảng thật trong **SQL Server** thông qua thư viện **Entity Framework Core 8.0 (Code-First)**, quản lý lịch sử thay đổi bảng (`Migrations`) và tự động nạp dữ liệu mẫu (`SeedData`).
2. **Thực thi Logic Nghiệp vụ (`Services`):** Chứa code xử lý thực tế cho các chức năng đã khai báo bên tầng `Application` (truy vấn danh sách sân, thuật toán kiểm tra trùng khung giờ đặt sân, tự động tính tiền theo giờ, đăng nhập/đăng ký).

---

## 2. Cấu trúc thư mục hiện tại

```text
SportsCourtBooking.Infrastructure/
├── Data/
│   ├── Migrations/                # Chứa các file tự động sinh ra để tạo bảng và dữ liệu mẫu trong SQL Server
│   ├── AppDbContext.cs            # Khai báo 6 bảng (DbSet), khóa ngoại, ràng buộc dữ liệu và Seed Data
│   └── AppDbContextFactory.cs     # Hỗ trợ công cụ dòng lệnh (dotnet ef) chạy Migration không bị lỗi Design-time
├── Services/
│   ├── AuthService.cs             # Thực thi logic Đăng nhập & Đăng ký tài khoản người dùng
│   ├── BookingService.cs          # Thực thi thuật toán kiểm tra trùng giờ đặt sân & tự động tính tổng tiền
│   └── CourtService.cs            # Thực thi truy vấn danh sách sân thể thao & lọc sân theo loại
├── README.md                      # Tài liệu hướng dẫn chi tiết cho các thành viên trong nhóm
└── SportsCourtBooking.Infrastructure.csproj
```
