# SportsCourtBooking.Web

Ứng dụng web ASP.NET Core MVC bằng C#. Project này là lớp trình bày: nhận yêu cầu HTTP, gọi các use case ở Application và trả về Razor views.

- `Program.cs`: điểm khởi động và cấu hình pipeline ASP.NET Core.
- `Controllers/`: controller điều phối yêu cầu web.
- `Views/`: giao diện Razor.
- `wwwroot/`: CSS, JavaScript phía trình duyệt, hình ảnh và các tài nguyên tĩnh khác.

Project nhắm đến `net8.0` và có prototype Razor cho danh sách sân, lịch chọn giờ, xem lại booking và màn hình tài khoản. Dữ liệu sân/giá hiện là mẫu; form đăng nhập/đăng ký chưa kết nối ASP.NET Core Identity và chưa tạo booking thật.
