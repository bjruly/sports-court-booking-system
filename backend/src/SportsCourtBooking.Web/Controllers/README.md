# Controllers

Chứa các ASP.NET Core MVC controller tiếp nhận yêu cầu, gọi use case từ Application và chọn view hoặc kết quả phản hồi phù hợp.


# SportsCourtBooking.Web - Controllers

## 1. Giới thiệu chung & Vai trò

Thư mục **`Controllers/`** chứa các lớp điều hướng (MVC Controllers) thuộc tầng giao diện `SportsCourtBooking.Web`.
Các Controller tại đây chịu trách nhiệm:

* Tiếp nhận yêu cầu (HTTP `GET` / `POST`) từ trình duyệt của người dùng.
* Gọi các dịch vụ nghiệp vụ (`ICourtService`, `IBookingService`, `IAuthService`) từ tầng `Application` để lấy hoặc lưu dữ liệu xuống SQL Server.
* Chuyển đổi dữ liệu từ `DTOs` sang `ViewModels` và trả về các trang giao diện Razor Views (`.cshtml`) tương ứng.

---

## 2. Danh sách các Controllers hiện tại

```text
Controllers/
├── AuthController.cs          # Xử lý luồng Đăng nhập (Login), Đăng ký (Register) và Đăng xuất (Logout)
├── BookingController.cs       # Xử lý luồng Đặt sân chuyên biệt và API kiểm tra trùng khung giờ (JSON)
├── HomeController.cs          # Điều hướng chính cho toàn bộ giao diện người dùng (Index, Schedule, Review, Login, Register)
└── README.md                  # Tài liệu hướng dẫn thư mục Controllers
```
