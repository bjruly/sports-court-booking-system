# Backend

Ứng dụng được xây dựng bằng C# trên ASP.NET Core MVC, theo modular monolith và phân lớp. Project Web phục vụ giao diện Razor và tiếp nhận yêu cầu HTTP; các project Application, Domain và Infrastructure tách biệt use case, quy tắc miền và tích hợp/lưu trữ.

## Thư mục và file liên quan

- `src/`: các project backend.
- `src/SportsCourtBooking.Web/`: ứng dụng ASP.NET Core MVC, controller, Razor views và tài nguyên tĩnh.
- `src/SportsCourtBooking.Application/`: điều phối nghiệp vụ và use case.
- `src/SportsCourtBooking.Domain/`: thực thể, quy tắc và khái niệm miền.
- `src/SportsCourtBooking.Infrastructure/`: tích hợp PostgreSQL, EF Core và dịch vụ ngoài.
- `tests/SportsCourtBooking.Tests/`: project kiểm thử xUnit.

## Chạy và kiểm tra

Cài .NET 8 SDK, sau đó chạy các lệnh từ thư mục gốc repository:

```powershell
dotnet restore backend/SportsCourtBooking.sln
dotnet build backend/SportsCourtBooking.sln
dotnet test backend/SportsCourtBooking.sln
dotnet run --project backend/src/SportsCourtBooking.Web
```

Web hiện là MVC scaffold mặc định; chưa có nghiệp vụ đặt sân hoặc kết nối cơ sở dữ liệu. Project xUnit đã được tạo để bổ sung kiểm thử cùng với các use case đầu tiên.
