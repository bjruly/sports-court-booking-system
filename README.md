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
- **Framework hiện tại:** .NET 8 (`net8.0`), EF Core 8 với **PostgreSQL** (Npgsql). Thư mục migration là `SportsCourtBooking.Infrastructure/Data/Migrations` (chưa có file nào, xem mục "Tạo migration PostgreSQL").
- **Cache/giữ chỗ:** Redis là thành phần tùy chọn theo đề xuất trong báo cáo.
- **Tích hợp dự kiến:** VNPay/MoMo, email/SMS và dịch vụ bản đồ.
- **Triển khai/chạy cục bộ:** Docker và Docker Compose (web + PostgreSQL 16 + pgAdmin).

Solution .NET đã được khởi tạo trong `backend/SportsCourtBooking.sln`, gồm các project Web, Application, Domain, Infrastructure và kiểm thử.

## Trạng thái triển khai

- Đã kết nối PostgreSQL: danh sách sân, lịch khung giờ, đăng ký, đăng nhập (cookie, mật khẩu băm PBKDF2) và tạo đơn đặt sân (trạng thái `Pending`) đều lưu/đọc từ database.
- Quy tắc đặt sân nằm tập trung ở `Application/Rules/BookingRules.cs`: giờ mở cửa 06:00–22:00, tối đa 4 giờ liên tiếp, phụ thu 50.000₫/giờ từ 17:00, không đặt giờ đã qua (tính theo giờ Việt Nam), chống đặt trùng khi nhiều người bấm cùng lúc.
- Chưa làm: thanh toán, đánh giá, hủy/hoàn tiền, cơ chế hết hạn giữ chỗ `Pending`, trang quản trị đầy đủ, khóa tài khoản khi nhập sai mật khẩu nhiều lần. Quận/huyện của sân hiện vẫn gán cứng theo Id sân trong `HomeController` (cần thêm cột vào bảng `Courts` để thay thế).

## Tính năng mục tiêu

- Khách hàng tìm kiếm cơ sở và sân, xem lịch trống, đặt sân, thanh toán, hủy đặt và đánh giá.
- Chủ sân quản lý cơ sở, sân, giá, lịch đặt, voucher và doanh thu.
- Quản trị viên duyệt cơ sở, quản lý người dùng/danh mục và theo dõi thống kê.
- Đơn đặt có cơ chế giữ chỗ tạm thời; thanh toán và trạng thái đơn được đồng bộ theo quy trình nghiệp vụ.

## Trình tự công việc tiếp theo

Thực hiện lần lượt theo các bước phụ thuộc nhau. Chỉ chuyển sang bước tiếp theo khi đầu ra của bước trước đã được rà soát. Sau khi hoàn tất và có lịch sử commit, nhóm cập nhật phần ghi nhận đóng góp dựa trên công việc thực tế.

### Phạm vi MVP đã chốt

- Khách cần có tài khoản và đăng nhập để đặt sân; người mới có thể đăng ký. Khách vãng lai/đặt hộ để giai đoạn sau.
- Luồng MVP: đăng nhập → xem danh sách sân → xem lịch trống và giá → chọn giờ → tạo booking `PENDING`.
- Mỗi khung giờ dài 1 giờ; một booking gồm tối đa 4 khung liên tiếp trên cùng một sân trong cùng ngày.
- Booking `PENDING` chưa phải là booking đã xác nhận hoặc đã thanh toán. Thanh toán thật, voucher, hủy/hoàn tiền và các tích hợp ngoài hệ thống nằm ngoài MVP.
- Thời hạn giữ `PENDING`, cơ chế hết hạn/nhả giờ và ràng buộc chống đặt trùng cần được rà soát về mặt nghiệp vụ, dữ liệu và kiểm thử trước khi triển khai.

| Bước                                  | Trình tự công việc                                                                                                                                                                                         | Điều kiện hoàn tất để chuyển bước                                                                                                                  |
| --------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1. Rà soát nghiệp vụ                | Dựa trên phạm vi MVP đã chốt, thống nhất luồng người dùng đầu-cuối, giá, vòng đời booking, thời hạn giữ chỗ, hết hạn và các trường hợp cạnh tranh.                              | Workflow và quy tắc nghiệp vụ nhất quán; thời hạn giữ chỗ và kết quả các trường hợp biên được xác định.                              |
| 2. Đồng bộ thiết kế và kiểm thử | Cập nhật ERD theo quyết định nghiệp vụ; hoàn thiện wireframe tối thiểu; viết tiêu chí nghiệm thu/test case theo luồng; review tính nhất quán, rồi commit các kết quả đã hoàn thành. | Tài liệu, luồng, schema, wireframe và tiêu chí nghiệm thu nhất quán; solution build được; lịch sử commit phản ánh các thay đổi thực tế. |

### Quy tắc ghi nhận đóng góp

- Sau khi từng hạng mục hoàn tất và được commit, cập nhật phần đóng góp theo công việc thực tế đã làm; xác nhận với thành viên liên quan, không suy đoán chỉ từ tên tác giả commit.
- Mọi thay đổi code cần build/test phù hợp và được review trước khi nhập nhánh chính; không đưa thông tin kết nối hoặc bí mật vào repository.

## Cấu trúc thư mục

```text
.
├── README.md
├── .gitignore
├── .dockerignore
├── .env.example
├── Dockerfile
├── docker-compose.yml
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
- `database/`: Tài liệu và tài sản liên quan đến cơ sở dữ liệu. Lịch sử migration thực tế của EF Core nằm trong `backend/src/SportsCourtBooking.Infrastructure/Data/Migrations`.
- `Dockerfile`, `docker-compose.yml`, `.dockerignore`, `.env.example`: cấu hình chạy ứng dụng web, PostgreSQL và pgAdmin bằng Docker; `.env` là file cục bộ của từng máy, không đưa lên repository.

Các bước chạy Docker bên dưới chạy toàn bộ hệ thống: web + PostgreSQL + pgAdmin. Lần đầu khởi động, ứng dụng tự tạo database và chạy migration.

## Chạy bằng Docker

Dự án có sẵn cấu hình Docker để mọi thành viên chạy cùng một môi trường (web ASP.NET Core + PostgreSQL), không cần cài .NET SDK hay PostgreSQL trên máy.

### Yêu cầu

- Docker Desktop (Windows cần bật WSL 2) và đã mở lên, đợi trạng thái **Engine running**.

### Các bước chạy

1. Clone hoặc `git pull` repo, mở terminal tại **thư mục gốc** (nơi có `docker-compose.yml`).
2. Tạo file `.env` từ file mẫu:
   ```bash
   copy .env.example .env      # Windows (PowerShell dùng được cả: cp .env.example .env)
   ```
3. Mở `.env`, đặt `POSTGRES_PASSWORD` và `PGADMIN_PASSWORD` (không dùng ký tự `;` `=` `$`; mật khẩu chỉ dùng trên máy mình).
4. Chạy:
   ```bash
   docker compose up --build
   ```
5. Đợi log hiện `Database đã sẵn sàng.` (lần đầu có thể mất vài chục giây để PostgreSQL khởi động), rồi mở [http://localhost:5101](http://localhost:5101).

### Tài khoản mẫu (chỉ dùng để thử trên máy)

| Vai trò         | Email                    | Mật khẩu       |
| ---------------- | ------------------------ | ---------------- |
| Quản trị viên | `admin@sportscourt.vn` | `Admin@123`    |
| Khách hàng     | `khachhang@gmail.com`  | `Customer@123` |

Bạn cũng có thể tự đăng ký tài khoản mới ở trang **Tạo tài khoản**. Đăng nhập xong, vào **Lịch đặt của tôi** để xem đơn vừa tạo. Tài khoản quản trị xem lịch của mọi khách ở `/Booking`.

### Lệnh thường dùng

| Mục đích                                       | Lệnh                         |
| ------------------------------------------------- | ----------------------------- |
| Chạy nền, không chiếm terminal                | `docker compose up -d`      |
| Dừng và xóa container                          | `docker compose down`       |
| Dừng và **xóa luôn dữ liệu database** | `docker compose down -v`    |
| Xem log                                           | `docker compose logs -f`    |
| Build lại sau khi đổi code                     | `docker compose up --build` |

### Xem dữ liệu bằng pgAdmin

1. Chạy `docker compose up` và đợi log hiện `Database đã sẵn sàng.`
2. Mở [http://localhost:5050](http://localhost:5050), đăng nhập bằng `PGADMIN_EMAIL` và `PGADMIN_PASSWORD` trong `.env`.
3. Chuột phải **Servers → Register → Server**, tab **Connection** điền:

| Ô                   | Giá trị                                                                 |
| -------------------- | ------------------------------------------------------------------------- |
| Host                 | `db` (không phải `localhost`, vì pgAdmin chạy trong mạng Docker) |
| Port                 | `5432`                                                                  |
| Maintenance database | `POSTGRES_DB` trong `.env` (mặc định `sportscourt`)              |
| Username / Password  | `POSTGRES_USER` / `POSTGRES_PASSWORD` trong `.env`                  |

4. Mở **Servers → (tên server) → Databases → sportscourt → Schemas → public → Tables**, chuột phải bảng → **View/Edit Data → All Rows**.

Tên bảng và cột do EF Core tạo theo dạng chữ hoa đầu (`"Users"`, `"Courts"`, `"Bookings"`...), nên khi viết SQL phải đặt trong dấu nháy kép, ví dụ `SELECT * FROM "Courts";`.

Muốn kết nối bằng công cụ cài trên máy (DBeaver, DataGrip...): host `localhost`, port `5432`, cùng user/mật khẩu như trên.

### Tạo migration PostgreSQL

Thư mục `Data/Migrations` hiện chưa có file nào (các migration cũ viết cho SQL Server đã bị xóa vì không chạy được trên PostgreSQL). Khi chưa có migration, ứng dụng tự tạo bảng và dữ liệu mẫu từ model bằng `EnsureCreated`. Để dùng migration (cần .NET 8 SDK và công cụ `dotnet-ef`):

```bash
dotnet ef migrations add InitialCreate --project backend/src/SportsCourtBooking.Infrastructure --startup-project backend/src/SportsCourtBooking.Web --output-dir Data/Migrations
```

Nếu database đã được tạo bằng `EnsureCreated` trước khi có migration, chạy `docker compose down -v` để xóa dữ liệu rồi `docker compose up --build` lại.

### Lưu ý

- **Không commit file `.env`** (đã nằm trong `.gitignore`); chỉ commit `.env.example`.
- Đã đặt `POSTGRES_PASSWORD` rồi thì đừng đổi giữa chừng; muốn đổi phải chạy `docker compose down -v` rồi chạy lại. Nếu trước đây bạn từng chạy bản cũ của dự án (dùng SQL Server), cũng nên chạy `docker compose down -v` một lần để xóa container và dữ liệu cũ.
- Web kết nối PostgreSQL qua biến `ConnectionStrings__DefaultConnection` trong `docker-compose.yml`. Khi chạy ngoài Docker (`dotnet run`), `appsettings.json` chỉ có host, database và user (không lưu mật khẩu); hãy đặt biến môi trường `ConnectionStrings__DefaultConnection` kèm mật khẩu, ví dụ `Host=localhost;Port=5432;Database=sportscourt;Username=postgres;Password=...`, và nhớ chạy `docker compose up -d db` trước.
- Nếu container `db` không chạy, kiểm tra `docker compose logs db`.
- Chạy kiểm thử đơn vị: `dotnet test backend/SportsCourtBooking.sln` (cần cài .NET 8 SDK).
- Nếu cổng `5101`, `5432` hoặc `5050` đã bị chương trình khác dùng, đổi số bên trái trong mục `ports` của `docker-compose.yml` (ví dụ `"5102:8080"`).
