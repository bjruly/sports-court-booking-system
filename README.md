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
- **Framework hiện tại:** .NET 8 (`net8.0`); cấu hình EF Core/Npgsql và PostgreSQL sẽ được bổ sung ở bước thiết lập lưu trữ dữ liệu.
- **Cache/giữ chỗ:** Redis là thành phần tùy chọn theo đề xuất trong báo cáo.
- **Tích hợp dự kiến:** VNPay/MoMo, email/SMS và dịch vụ bản đồ.

Solution .NET đã được khởi tạo trong `backend/SportsCourtBooking.sln`, gồm các project Web, Application, Domain, Infrastructure và kiểm thử.

## Tính năng nghiệp vụ

- Khách hàng tìm kiếm cơ sở và sân, xem lịch trống, đặt sân, thanh toán, hủy đặt và đánh giá.
- Chủ sân quản lý cơ sở, sân, giá, lịch đặt, voucher và doanh thu.
- Quản trị viên duyệt cơ sở, quản lý người dùng/danh mục và theo dõi thống kê.
- Đơn đặt có cơ chế giữ chỗ tạm thời; thanh toán và trạng thái đơn được đồng bộ theo quy trình nghiệp vụ.

## Trình tự công việc tiếp theo

Thực hiện lần lượt theo các bước phụ thuộc nhau. Chỉ chuyển sang bước tiếp theo khi đầu ra của bước trước đã được rà soát. Sau khi hoàn tất và có lịch sử commit, nhóm cập nhật phần ghi nhận đóng góp dựa trên công việc thực tế.

### Phạm vi MVP đã chốt

- Khách phải đăng nhập để đặt sân; khách vãng lai và đặt hộ để giai đoạn sau.
- Luồng MVP: đăng nhập → xem danh sách sân → xem lịch trống và giá → chọn giờ → tạo booking `PENDING`.
- Mỗi khung giờ dài 1 giờ; một booking gồm tối đa 4 khung liên tiếp trên cùng một sân trong cùng ngày.
- Booking `PENDING` chưa phải là booking đã xác nhận hoặc đã thanh toán. Thanh toán thật, voucher, hủy/hoàn tiền và các tích hợp ngoài hệ thống nằm ngoài MVP.
- Thời hạn giữ `PENDING`, cơ chế hết hạn/nhả giờ và ràng buộc chống đặt trùng cần được rà soát về mặt nghiệp vụ, dữ liệu và kiểm thử trước khi triển khai.

| Bước | Trình tự công việc | Điều kiện hoàn tất để chuyển bước |
|---|---|---|
| 1. Rà soát nghiệp vụ | Dựa trên phạm vi MVP đã chốt, thống nhất luồng người dùng đầu-cuối, giá, vòng đời booking, thời hạn giữ chỗ, hết hạn và các trường hợp cạnh tranh. | Workflow và quy tắc nghiệp vụ nhất quán; thời hạn giữ chỗ và kết quả các trường hợp biên được xác định. |
| 2. Đồng bộ thiết kế và kiểm thử | Cập nhật ERD theo quyết định nghiệp vụ; hoàn thiện wireframe tối thiểu; viết tiêu chí nghiệm thu/test case theo luồng; review tính nhất quán, rồi commit các kết quả đã hoàn thành. | Tài liệu, luồng, schema, wireframe và tiêu chí nghiệm thu nhất quán; solution build được; lịch sử commit phản ánh các thay đổi thực tế. |

### Quy tắc ghi nhận đóng góp

- Sau khi từng hạng mục hoàn tất và được commit, cập nhật phần đóng góp theo công việc thực tế đã làm; xác nhận với thành viên liên quan, không suy đoán chỉ từ tên tác giả commit.
- Mọi thay đổi code cần build/test phù hợp và được review trước khi nhập nhánh chính; không đưa thông tin kết nối hoặc bí mật vào repository.

## Cấu trúc thư mục

```text
.
├── README.md
├── .gitignore
├── docs/
│   └── ...
├── backend/
│   ├── SportsCourtBooking.sln
│   ├── README.md
│   ├── src/
│   │   ├── SportsCourtBooking.Web/
│   │   ├── SportsCourtBooking.Application/
│   │   ├── SportsCourtBooking.Domain/
│   │   └── SportsCourtBooking.Infrastructure/
│   └── tests/
│       └── SportsCourtBooking.Tests/
├── database/
│   ├── README.md
│   └── migrations/
```

### Vai trò các thư mục

- `docs/`: Báo cáo và sơ đồ phân tích, thiết kế hệ thống.
- `backend/`: Solution .NET 8; `SportsCourtBooking.Web/` chứa ứng dụng MVC, các project Application, Domain, Infrastructure phân chia trách nhiệm nghiệp vụ và tích hợp; `tests/` chứa project kiểm thử xUnit.
- `database/`: Tài liệu và tài sản liên quan đến PostgreSQL; `migrations/` dành cho lịch sử thay đổi schema.

Ứng dụng web hiện dùng trang MVC mẫu để xác nhận solution có thể chạy; các nghiệp vụ đặt sân và cấu hình cơ sở dữ liệu sẽ được triển khai ở các bước tiếp theo.
