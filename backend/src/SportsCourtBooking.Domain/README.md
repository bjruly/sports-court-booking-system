# Domain

Chứa mô hình cốt lõi và các quy tắc nghiệp vụ độc lập với framework, bao gồm các khái niệm như người dùng, cơ sở, sân, khung giờ, đơn đặt và thanh toán.


# SportsCourtBooking.Domain

## 1. Giới thiệu chung

Đây là tầng **Domain (Core)** của hệ thống Đặt sân Thể thao (`SportsCourtBooking`). Tầng này chứa các thực thể nghiệp vụ cốt lõi (Entities) và các kiểu dữ liệu liệt kê (Enums), đóng vai trò là nền tảng dữ liệu cho toàn bộ các tầng khác (`Application`, `Infrastructure`, `Web`).

## 2. Cấu trúc thư mục hiện tại

```text
SportsCourtBooking.Domain/
├── Entities/                  # Chứa các lớp thực thể ánh xạ với sơ đồ ERD (05_ERD.drawio)
│   ├── BaseEntity.cs          # Lớp cơ sở dùng chung (Id, CreatedAt, UpdatedAt)
│   ├── User.cs                # Thực thể Người dùng (Khách hàng, Nhân viên, Admin)
│   ├── CourtType.cs           # Thực thể Loại sân (Cầu lông, Bóng đá, Tennis, Pickleball...)
│   ├── Court.cs               # Thực thể Sân thể thao (Thông tin sân, giá theo giờ, trạng thái)
│   ├── Booking.cs             # Thực thể Đơn đặt sân (Ngày đặt, giờ bắt đầu - kết thúc, tổng tiền)
│   ├── Payment.cs             # Thực thể Thanh toán của đơn đặt sân
│   └── Review.cs              # Thực thể Đánh giá & Nhận xét sân của khách hàng
├── Enums/                     # Chứa các hằng số trạng thái của hệ thống
│   └── DomainEnums.cs         # UserRole, CourtStatus, BookingStatus, PaymentStatus
├── README.md                  # Tài liệu hướng dẫn tầng Domain
└── SportsCourtBooking.Domain.csproj
```
