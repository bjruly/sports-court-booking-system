# Backend

Ứng dụng được xây dựng bằng C# trên ASP.NET Core MVC, theo modular monolith và phân lớp. Project Web phục vụ giao diện Razor và tiếp nhận yêu cầu HTTP; các project Application, Domain và Infrastructure tách biệt use case, quy tắc miền và tích hợp/lưu trữ.

## Thư mục và file liên quan

- `src/`: các project backend.
- `src/SportsCourtBooking.Web/`: ứng dụng ASP.NET Core MVC, controller, Razor views và tài nguyên tĩnh.
- `src/SportsCourtBooking.Application/`: điều phối nghiệp vụ và use case.
- `src/SportsCourtBooking.Domain/`: thực thể, quy tắc và khái niệm miền.
- `src/SportsCourtBooking.Infrastructure/`: tích hợp PostgreSQL, EF Core và dịch vụ ngoài.
- `tests/`: kiểm thử tự động cho backend.

Các file hiện có là file khung trống; project .NET, phiên bản SDK và cấu hình build chưa được khởi tạo.
