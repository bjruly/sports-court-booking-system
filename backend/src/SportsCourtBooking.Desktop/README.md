# SportsCourtBooking.Desktop

Ứng dụng WinForms (.NET 8) dành cho **quản trị viên**, thay cho dashboard `/Admin` trước đây trên web. Khách hàng, chủ sân và nhân viên vẫn dùng trang web.

Ứng dụng gọi thẳng các service ở tầng Application/Infrastructure và kết nối trực tiếp PostgreSQL, nên dùng chung quy tắc nghiệp vụ và dữ liệu với web.

## Chạy

Cần Windows và .NET 8 SDK. Từ thư mục gốc repository:

```powershell
docker compose up -d
dotnet run --project backend/src/SportsCourtBooking.Desktop
```

Database phải được web khởi tạo trước (migration chạy khi web khởi động). Đăng nhập bằng tài khoản quản trị viên mẫu trong [README chính](../../../README.md).

## Cấu hình kết nối

Lấy theo thứ tự:

1. Biến môi trường `ConnectionStrings__DefaultConnection`, rồi `ConnectionStrings:DefaultConnection` trong `appsettings.json` cạnh file chạy.
2. File `.env` ở thư mục gốc repository, kết nối tới `localhost:5432`.

Không ghi mật khẩu thật vào `appsettings.json` rồi commit. Vì ứng dụng nối thẳng database, chỉ cài trên máy của quản trị viên và không mở cổng PostgreSQL ra Internet.

## Cấu trúc

- `Program.cs`, `LoginForm.cs`, `MainForm.cs`: khởi động, đăng nhập, cửa sổ chính.
- `PageCatalog.cs`: danh sách màn hình.
- `Core/`: gọi service (`AppServices`), cấu hình kết nối, định dạng, giao diện chung.
- `Controls/`: bảng, ô số liệu, biểu đồ, in báo cáo.
- `Dialogs/`: hộp thoại nhập liệu.
- `Pages/Admin/`: từng màn hình.

Các màn hình: thống kê hệ thống, lịch đặt theo ngày, hoàn tiền, khách hàng, chủ sân, cơ sở, môn thể thao, đánh giá, báo cáo tổng hợp, báo cáo danh mục.

Phím tắt: **F5** tải lại màn hình; bấm chuột phải vào một dòng để mở các thao tác; bấm tiêu đề cột để sắp xếp.
