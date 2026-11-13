# SportsCourtBooking.Web

Ứng dụng web ASP.NET Core MVC bằng C#. Project này là lớp trình bày: nhận yêu cầu HTTP, gọi các use case ở Application và trả về Razor views.

- `Program.cs`: điểm khởi động và cấu hình pipeline ASP.NET Core.
- `Controllers/`: controller điều phối yêu cầu web.
- `Views/`: giao diện Razor.
- `wwwroot/`: CSS, JavaScript phía trình duyệt, hình ảnh và các tài nguyên tĩnh khác.

Project nhắm đến `net8.0` và có prototype Razor cho danh sách sân, lịch chọn giờ, xem lại booking và màn hình tài khoản. Dữ liệu sân/giá hiện là mẫu; form đăng nhập/đăng ký chưa kết nối ASP.NET Core Identity và chưa tạo booking thật.

## 1. Giới thiệu chung & Vai trò trong đồ án

Đây là tầng **Presentation / Web UI (`SportsCourtBooking.Web`)** của hệ thống Đặt sân Thể thao, được xây dựng trên nền tảng **ASP.NET Core 8.0 MVC**.
Tầng này là điểm khởi chạy chính (`Startup Project`) của toàn bộ ứng dụng:

* Chứa cấu hình khởi tạo hệ thống, chuỗi kết nối PostgreSQL và đăng ký Dependency Injection (`Program.cs`, `appsettings.json`).
* Chứa hệ thống `Controllers`, `Models` (`ViewModels`), giao diện người dùng `Views` (`.cshtml`) và tài nguyên tĩnh `wwwroot` (CSS, JS, Bootstrap).

---

## 2. Cấu trúc thư mục hiện tại

```text
SportsCourtBooking.Web/
├── Controllers/               # Chứa các MVC Controllers điều hướng và xử lý request
│   ├── AuthController.cs      # Xử lý পোস্ট Đăng nhập, Đăng ký, Đăng xuất
│   ├── BookingController.cs   # Xử lý đặt sân và API kiểm tra giờ trống
│   ├── HomeController.cs      # Điều hướng trang chủ, lịch đặt sân, xác nhận đặt sân
│   └── README.md
├── Models/                    # Chứa các ViewModel phục vụ hiển thị dữ liệu lên Razor Views
│   ├── BookingPageViewModel.cs
│   ├── CourtCatalogViewModel.cs
│   └── ErrorViewModel.cs
├── Properties/
│   └── launchSettings.json    # Cấu hình cổng chạy mặc định khi phát triển (Port 5101)
├── Views/                     # Chứa giao diện người dùng (Razor Views)
│   ├── Home/                  # Index.cshtml, Schedule.cshtml, Review.cshtml, Login.cshtml, Register.cshtml
│   └── Shared/                # _Layout.cshtml (Khung giao diện chung), Error.cshtml
├── wwwroot/                   # Chứa file tĩnh: css/site.css, js/site.js, thư viện Bootstrap & jQuery
├── appsettings.json           # Cấu hình chuỗi kết nối PostgreSQL (DefaultConnection)
├── Program.cs                 # Cấu hình dịch vụ (DbContext, Services) và HTTP Pipeline
├── README.md                  # Tài liệu hướng dẫn tầng Web
└── SportsCourtBooking.Web.csproj
```
