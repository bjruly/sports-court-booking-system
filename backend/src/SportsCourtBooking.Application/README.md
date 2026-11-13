# Application

Chứa các use case và điều phối nghiệp vụ của hệ thống, ví dụ xác thực, danh mục sân, đặt sân, thanh toán, khuyến mãi, đánh giá, báo cáo và thông báo.

Lớp này không phụ thuộc trực tiếp vào chi tiết lưu trữ hoặc giao diện HTTP.


# SportsCourtBooking.Application

## 1. Giới thiệu chung & Vai trò trong đồ án

Đây là tầng **Application (Ứng dụng)** của hệ thống Đặt sân Thể thao (`SportsCourtBooking`).
Tầng này đóng vai trò là **cầu nối trung gian** giữa giao diện `Web` (Controllers/Views) và tầng dữ liệu (`Domain` / `Infrastructure`):

* Định nghĩa các gói dữ liệu truyền tải (**DTOs**) để không làm lộ trực tiếp các bảng Database (`Entities`) lên giao diện.
* Định nghĩa bản thiết kế các chức năng nghiệp vụ (**Interfaces**) như: lấy danh sách sân, kiểm tra trùng giờ đặt sân, tính tiền tự động, đăng nhập và đăng ký.

---

## 2. Cấu trúc thư mục hiện tại

```text
SportsCourtBooking.Application/
├── DTOs/
│   ├── AuthDto.cs             # Khuôn mẫu dữ liệu Đăng nhập (LoginRequest), Đăng ký (RegisterRequest) và Thông tin User (UserDto)
│   ├── BookingDto.cs          # Khuôn mẫu dữ liệu tạo đơn đặt sân (CreateBookingRequest) và hiển thị lịch đặt (BookingDto)
│   └── CourtDto.cs            # Khuôn mẫu dữ liệu hiển thị thông tin sân thể thao kèm tên loại sân
├── Interfaces/
│   └── IServices.cs           # Khai báo 3 Interface chính: ICourtService, IBookingService, IAuthService
├── README.md                  # Tài liệu hướng dẫn tầng Application
└── SportsCourtBooking.Application.csproj
```
