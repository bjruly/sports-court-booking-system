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
## Kế hoạch 2 tuần (05/11–18/11/2026)

Mục tiêu cuối đợt: có lát cắt nghiệp vụ đầu tiên chạy được từ đăng nhập, xem danh sách sân và khung giờ trống đến tạo yêu cầu đặt sân ở trạng thái chờ; ngăn hai đơn đặt trùng khung giờ ở tầng cơ sở dữ liệu và có kiểm thử tự động. Chưa tích hợp cổng thanh toán thật, SMS/email hoặc Redis trong đợt này.

### Phân công đề xuất

Phân công theo mảng để làm song song; nhóm có thể hoán đổi theo kinh nghiệm thực tế. Mỗi thay đổi cần được ít nhất một thành viên khác review trước khi nhập nhánh chính.

| Thành viên | Phụ trách chính | Phối hợp |
|---|---|---|
| Trần Lê Gia Bảo | Điều phối, MVC và tích hợp luồng tổng thể | Chốt phạm vi, review UI và tích hợp |
| Huỳnh Nhật Quang | PostgreSQL, EF Core, schema và migration | Review ràng buộc dữ liệu, hỗ trợ kiểm thử tích hợp |
| Nguyễn Thanh Tuấn | Domain/Application, quy tắc lịch và tạo booking | Review nghiệp vụ và xử lý xung đột |
| Đào Hữu Tài | Razor UI: danh sách sân, lịch trống và biểu mẫu đặt | Phối hợp nối UI với use case |
| Tô Thành Nhân | Kiểm thử, dữ liệu mẫu, hướng dẫn chạy và checklist demo | Review chéo, theo dõi lỗi và chất lượng |

### Lịch công việc

| Ngày | Công việc và người phụ trách | Kết quả cần đạt |
|---|---|---|
| 05/11 (T5) | Cả nhóm chốt phạm vi và tiêu chí nghiệm thu; Bảo hỏi giảng viên về target framework; Nhân ghi quyết định và tạo checklist. | Chốt .NET 8 hay chuyển .NET 10 trước khi phát triển nghiệp vụ; solution restore/build được trên máy các thành viên. |
| 06/11 (T6) | Tuấn đề xuất vòng đời booking; Quang phác thảo schema; Tài dựng wireframe nhanh; Bảo điều phối review; Nhân viết kịch bản nghiệm thu. | Thống nhất ERD tối thiểu, trạng thái booking, quy tắc khung giờ và luồng người dùng. |
| 09/11 (T2) | Quang cấu hình PostgreSQL/EF Core và migration đầu; Tuấn tạo entity/domain rule; Tài dựng trang danh sách sân; Bảo tích hợp khung MVC; Nhân chuẩn bị dữ liệu mẫu. | Migration chạy được trên database rỗng; trang danh sách sân hiển thị dữ liệu mẫu. |
| 10/11 (T3) | Quang thêm ràng buộc chống lịch trùng; Tuấn viết use case truy vấn lịch trống; Tài hoàn thiện trang chi tiết sân; Bảo xử lý cấu hình Identity/cookie tối thiểu; Nhân bắt đầu unit test. | Có dữ liệu sân và khung giờ; người dùng có thể đăng nhập bằng tài khoản phát triển. |
| 11/11 (T4) | Tuấn triển khai use case tạo booking chờ; Quang hoàn thiện transaction/ràng buộc dữ liệu; Tài làm biểu mẫu chọn giờ; Bảo nối controller; Nhân kiểm thử quy tắc nghiệp vụ. | Luồng tạo booking hoạt động với trường hợp hợp lệ; booking lưu đúng trạng thái chờ. |
| 12/11 (T5) | Bảo và Tài nối liền các trang; Tuấn xử lý kiểm tra đầu vào; Quang kiểm tra migration trên database mới; Nhân thêm kiểm thử cho giờ không hợp lệ và sân không tồn tại. | Người dùng đăng nhập, chọn sân/giờ và gửi yêu cầu đặt; lỗi đầu vào được hiển thị rõ. |
| 13/11 (T6) | Quang và Nhân kiểm thử hai yêu cầu đặt đồng thời; Tuấn sửa lỗi quy tắc; Bảo rà soát luồng tích hợp; Tài hoàn thiện trạng thái thành công/thất bại. | Không thể tạo hai booking chồng lấn cho cùng sân; kiểm thử tái hiện được xung đột. |
| 16/11 (T2) | Cả nhóm xử lý lỗi theo ưu tiên; Bảo rà soát luồng đầu-cuối; Quang kiểm tra migration; Tuấn kiểm tra trạng thái; Tài kiểm tra giao diện; Nhân chạy lại bộ test. | Build và các test mục tiêu chạy ổn định từ database mới. |
| 17/11 (T3) | Nhân cập nhật hướng dẫn chạy/test; Tài và Bảo chuẩn bị demo; Quang/Tuấn review schema và quy tắc; cả nhóm review chéo pull request. | Có hướng dẫn cài đặt, dữ liệu demo và checklist trình diễn. |
| 18/11 (T4) | Cả nhóm chạy nghiệm thu, demo, ghi nhận lỗi còn lại và thống nhất backlog đợt tiếp theo. | Demo được luồng xem sân → xem giờ trống → tạo booking chờ; có danh sách việc tiếp theo và các giới hạn đã biết. |

### Tiêu chí nghiệm thu và nhịp làm việc

- Mỗi ngày cập nhật tiến độ ngắn: đã xong, việc tiếp theo, trở ngại; báo sớm nếu phụ thuộc giữa các phần bị tắc.
- Mọi thay đổi được review trước khi nhập nhánh chính; không để bí mật hoặc thông tin kết nối thật trong repository.
- Tối thiểu có unit test cho quy tắc booking và kiểm thử tích hợp cho lưu booking, migration và xung đột lịch.
- Chỉ xem đợt này hoàn thành khi solution build được, hướng dẫn chạy từ database mới làm theo được và demo không cần thanh toán thật.
- Nếu giảng viên yêu cầu cố định .NET 8, giữ target đó theo yêu cầu môn học; nếu không, ưu tiên đánh giá chuyển .NET 10 ngay ngày 05/11 để tránh nâng cấp giữa chừng.


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
