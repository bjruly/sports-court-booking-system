# Hệ thống đặt sân thể thao

Đồ án môn học **Ngôn ngữ lập trình C#**, xây dựng hệ thống đặt sân thể thao trực tuyến với giao diện web cho khách hàng, chủ sân và quản trị viên.

- **Giảng viên hướng dẫn:** TS. Đỗ Như Tài
- **Nhóm sinh viên:** 9
  - 3124411038 - Trần Lê Gia Bảo
  - 3124411245 - Huỳnh Nhật Quang
  - 3124411337 - Nguyễn Thanh Tuấn
  - 3124411261 - Đào Hữu Tài
  - 3124411198 - Tô Thành Nhân

## Kiến trúc và công nghệ

Đây là dự án C# sử dụng **ASP.NET Core MVC + Razor** cho ứng dụng web, tổ chức theo kiến trúc **modular monolith** và các lớp nghiệp vụ. Báo cáo trong `docs/` có khảo sát Next.js/React như một lựa chọn frontend; bộ khung triển khai hiện tại chọn MVC để ứng dụng tập trung vào nền tảng .NET/C#.

- **Ứng dụng web:** ASP.NET Core MVC, Razor và C#.
- **Nghiệp vụ:** các lớp Application và Domain bằng C#.
- **Truy cập dữ liệu:** Entity Framework Core và Npgsql.
- **Cơ sở dữ liệu:** PostgreSQL.
- **Cache/giữ chỗ:** Redis là thành phần tùy chọn theo đề xuất trong báo cáo.
- **Tích hợp dự kiến:** VNPay/MoMo, email/SMS và dịch vụ bản đồ.

Cấu hình .NET SDK, phiên bản framework và các phụ thuộc sẽ được chốt khi khởi tạo project.

## Tính năng nghiệp vụ

- Khách hàng tìm kiếm cơ sở và sân, xem lịch trống, đặt sân, thanh toán, hủy đặt và đánh giá.
- Chủ sân quản lý cơ sở, sân, giá, lịch đặt, voucher và doanh thu.
- Quản trị viên duyệt cơ sở, quản lý người dùng/danh mục và theo dõi thống kê.
- Đơn đặt có cơ chế giữ chỗ tạm thời; thanh toán và trạng thái đơn được đồng bộ theo quy trình nghiệp vụ.

## Cấu trúc thư mục

```text
.
├── README.md
├── docs/
│   ├── README.md
│   ├── 01_C4_Context.drawio
│   ├── 02_C4_Container.drawio
│   ├── 03_Workflow.drawio
│   ├── 04_UseCase.drawio
│   ├── 05_ERD.drawio
│   └── BaoCao_Nhom_DatSanTheThao.docx
├── backend/
│   ├── README.md
│   ├── src/
│   │   ├── README.md
│   │   ├── SportsCourtBooking.Web/
│   │   │   ├── README.md
│   │   │   ├── Program.cs
│   │   │   ├── Controllers/
│   │   │   │   ├── README.md
│   │   │   │   └── HomeController.cs
│   │   │   ├── Views/
│   │   │   │   ├── README.md
│   │   │   │   ├── Home/
│   │   │   │   │   ├── README.md
│   │   │   │   │   └── Index.cshtml
│   │   │   │   └── Shared/
│   │   │   │       ├── README.md
│   │   │   │       └── _Layout.cshtml
│   │   │   └── wwwroot/
│   │   │       └── README.md
│   │   ├── SportsCourtBooking.Application/
│   │   │   └── README.md
│   │   ├── SportsCourtBooking.Domain/
│   │   │   └── README.md
│   │   └── SportsCourtBooking.Infrastructure/
│   │       └── README.md
│   └── tests/
│       └── README.md
└── database/
    ├── README.md
    └── migrations/
        └── README.md
```

### Vai trò các thư mục

- `docs/`: Báo cáo và sơ đồ phân tích, thiết kế hệ thống.
- `backend/`: Ứng dụng ASP.NET Core MVC và các lớp nghiệp vụ C#; `SportsCourtBooking.Web/` chứa controller, Razor views và tài nguyên tĩnh; các project Application, Domain, Infrastructure đảm nhiệm use case, quy tắc miền và tích hợp/lưu trữ; `tests/` dành cho kiểm thử tự động.
- `database/`: Tài liệu và tài sản liên quan đến PostgreSQL; `migrations/` dành cho lịch sử thay đổi schema.

Các file mã nguồn trong cây hiện là **file khung trống**, chưa phải ứng dụng có thể chạy. Nội dung triển khai và cấu hình build sẽ được bổ sung sau.
