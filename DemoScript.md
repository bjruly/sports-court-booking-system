# Kịch bản Demo & Thuyết trình Đồ án C# (Demo Script)
### Đề tài: Hệ thống Đặt Sân Thể Thao Trực Tuyến (Sân Nhà)

> **Giảng viên hướng dẫn:** TS. Đỗ Như Tài\
> **Nhóm thực hiện:** Nhóm 9 (5 thành viên)\
> • 3124411038 - Trần Lê Gia Bảo\
> • 3124411245 - Huỳnh Nhật Quang\
> • 3124411337 - Nguyễn Thanh Tuấn\
> • 3124411261 - Đào Hữu Tài\
> • 3124411198 - Tô Thành Nhân\
> **Thời lượng demo chuẩn:** 6 – 8 phút (cộng thời gian Q&A phản biện)

---

## 1. Phân chia Vai trò 5 Thành viên

| Thành viên | Nhiệm vụ chính trong buổi demo | Nội dung phụ trách |
| :--- | :--- | :--- |
| **Thành viên 1** (Trưởng nhóm) | **Mở đầu & Giới thiệu Kiến trúc** | Giới thiệu thành viên, mục tiêu đề tài, kiến trúc Clean Architecture / Modular Monolith. |
| **Thành viên 2** | **Demo Luồng Khách hàng (User Flow)** | Trực tiếp thao tác: Tìm sân, lọc theo môn/ngày, xem bảng giá và lịch trống. |
| **Thành viên 3** | **Demo Đặt sân & Quy tắc nghiệp vụ** | Chọn khung giờ, giải thích quy tắc giờ cao điểm, giới hạn 4 giờ, tạo đơn `Pending`. |
| **Thành viên 4** | **Demo Luồng Quản trị (Admin Flow)** | Đăng nhập Admin, trang Quản lý lịch đặt, lọc theo ngày/sân, cơ chế Background Worker 10 phút. |
| **Thành viên 5** | **Giải pháp Kỹ thuật & Kết quả Test** | Thuyết trình Concurrency (Advisory Lock, GiST Constraint), 107 test tự động (unit và service với EF InMemory) và test tích hợp PostgreSQL, Docker, kết luận. |

*(Nếu nhóm phân công 1 người nói và 1 người thao tác máy chiếu, có thể linh hoạt theo kịch bản dưới đây).*

---

## 2. Kịch bản Chi tiết Từng Bước (Kèm Lời thoại & Thao tác)

### ⏱️ Phần 1: Mở đầu & Tổng quan Công nghệ (1 phút)
* **Thao tác màn hình:** Chiếu slide trang bìa hoặc mở trang chủ `http://localhost:5101`.
* **Lời thoại gợi ý (Thành viên 1):**
  > *"Kính thưa Thầy Đỗ Như Tài, hôm nay Nhóm 9 xin đại diện báo cáo và demo đồ án môn học Ngôn ngữ lập trình C# với đề tài: **Hệ thống đặt sân thể thao trực tuyến - Sân Nhà**.*\
  > *Dự án của nhóm được xây dựng trên nền tảng **.NET 8** và ngôn ngữ **C# 12**, áp dụng mô hình **Modular Monolith** kết hợp nguyên lý **Clean Architecture** tách biệt 4 project: Domain, Application, Infrastructure và Web.*\
  > *Hệ thống sử dụng **PostgreSQL 16** làm cơ sở dữ liệu quan hệ, kết nối qua **Entity Framework Core 8**, và toàn bộ ứng dụng được container hóa đồng nhất bằng **Docker Compose**."*

---

### ⏱️ Phần 2: Luồng Khách hàng - Tìm kiếm & Xem Lịch trống (1.5 phút)
* **Thao tác màn hình (Thành viên 2):**
  1. Mở trang chủ: `http://localhost:5101`
  2. Tại thanh tìm kiếm, chọn môn **Cầu lông**, chọn ngày chơi là **ngày mai** (hoặc ngày bất kỳ phía trước), bấm **Tìm sân**.
  3. Chỉ vào danh sách kết quả (hiện ra các sân cầu lông VIP).
  4. Bấm vào thẻ **"Sân Cầu Lông VIP 1"** để chuyển đến trang xem lịch (`/Home/Schedule/1`).
* **Lời thoại gợi ý (Thành viên 2):**
  > *"Trước hết, em xin phép demo trải nghiệm của Khách hàng tìm sân. Ngay tại trang chủ, khách hàng có thể lọc nhanh theo bộ môn thể thao và ngày muốn chơi.*\
  > *Khi bấm vào một sân cụ thể, hệ thống sẽ tải bảng lưới lịch biểu từ 06:00 sáng đến 22:00 đêm.*\
  > *Tại đây có 2 điểm nghiệp vụ quan trọng được nhóm cài đặt:*\
  > *1. **Chặn giờ quá khứ:** Hệ thống tự động so sánh với giờ hiện tại của Việt Nam (`VietnamClock`); những khung giờ đã trôi qua sẽ bị khóa và hiển thị trạng thái 'Đã qua'.*\
  > *2. **Bảng giá động theo khung giờ cao điểm:** Từ 17:00 trở đi, hệ thống tự động cộng thêm phụ thu 50.000₫/giờ theo quy định, hiển thị trực quan giá từng ô để người chơi nắm rõ trước khi đặt."*

---

### ⏱️ Phần 3: Đặt sân, Review Chi phí & Tạo Đơn (2 phút)
* **Thao tác màn hình (Thành viên 3):**
  1. Thử cố tình chọn 5 ô liên tiếp $\rightarrow$ Hệ thống hiện thông báo lỗi: *"Mỗi lượt đặt tối đa 4 khung giờ liên tiếp"*.
  2. Chọn đúng 2 khung giờ liên tiếp (ví dụ: `18:00 – 19:00` và `19:00 – 20:00`).
  3. Bấm nút **"Tiếp tục đặt sân"** $\rightarrow$ Hệ thống chuyển sang trang **Review** (`/Home/Review`).
  4. Nếu chưa đăng nhập, bấm **Đăng nhập để tiếp tục** tại trang Xem lại:
     - Hệ thống mở trang Đăng nhập (`/Home/Login?returnUrl=...`) với returnUrl giữ lựa chọn.
     - Đăng nhập tài khoản khách: `khachhang@gmail.com` / `Customer@123`.
     - Sau khi đăng nhập thành công, hệ thống giữ lựa chọn và quay về trang **Xem lại**; bấm **Xác nhận đặt sân** để tạo đơn rồi chuyển đến **"Lịch đặt của tôi" (`/Booking/My`)**.
* **Lời thoại gợi ý (Thành viên 3):**
  > *"Tiếp theo là quy trình đặt sân và kiểm tra tính hợp lệ:*\
  > *Nhóm cài đặt quy tắc nghiệp vụ tập trung ở `BookingRules.cs`: người dùng chỉ được đặt tối đa 4 khung giờ liên tiếp. Nếu cố tình chọn sai, hệ thống sẽ chặn ngay từ giao diện lẫn ở tầng Server Controller.*\
  > *Tại trang Xác nhận đặt sân, người dùng xem lại chi tiết sân, ngày, khung giờ và tổng tiền được tính tự động.*\
  > *Hệ thống bắt buộc xác thực khi tạo đơn. Với cơ chế `returnUrl` an toàn, sau khi đăng nhập xong khách hàng quay về trang Xem lại, bấm Xác nhận đặt sân để tạo đơn và chuyển về trang 'Lịch đặt của tôi'. Đơn hàng vừa tạo lập tức mang trạng thái **Pending (Chờ xác nhận)**."*

---

### ⏱️ Phần 4: Góc nhìn Quản trị & Cơ chế Giữ chỗ 10 phút (1.5 phút)
* **Thao tác màn hình (Thành viên 4):**
  1. Mở ứng dụng WinForms: `dotnet run --project backend/src/SportsCourtBooking.Desktop`.
  2. Tại cửa sổ đăng nhập, nhập tài khoản Admin:
     - Email: `admin@sportscourt.vn`
     - Mật khẩu: `Admin@123`
  3. Ở menu trái, bấm **Lịch đặt theo ngày** (Staff dùng trang **Quản lý lịch đặt** `/Booking` trên web).
  4. Màn hình lịch đặt hiện ra:
     - Chọn đúng ngày vừa đặt đơn ở Bước 3.
     - Danh sách hiện ra đơn đặt của khách `Nguyễn Văn Khách`, hiển thị sân, giờ, số tiền và trạng thái `Pending`.
     - Thử chọn bộ lọc sân để lọc danh sách.
* **Lời thoại gợi ý (Thành viên 4):**
  > *"Bây giờ em xin chuyển sang góc nhìn của Quản trị viên / Nhân viên.*\
  > *Phần quản trị viên là ứng dụng Windows Forms riêng, dùng chung service và database với web, chỉ nhận tài khoản Admin. Nhân viên và chủ sân vẫn làm việc trên web theo Role-Based Access Control.*\
  > *Tại đây, nhân viên có thể tra cứu toàn bộ đơn đặt sân của khách theo từng ngày và lọc theo từng sân cụ thể.*\
  > *Về cơ chế giữ chỗ: Nhóm thiết kế đơn đặt mới có thời hạn giữ chỗ là **10 phút** (`ExpiresAt = CreatedAt + 10 phút`).*\
  > *Để tự động hóa việc này, nhóm xây dựng một **BackgroundService** mang tên `PendingBookingExpirationWorker` chạy ngầm mỗi 15 giây. Worker này sử dụng phương thức `ExecuteUpdateAsync` của EF Core 8 để tự động chuyển các đơn quá hạn sang trạng thái `Expired`, giải phóng khung giờ cho khách khác mà không cần nhân viên phải thao tác thủ công."*

---

### ⏱️ Phần 5: Điểm sáng Kỹ thuật, Unit Tests & Tổng kết (1.5 phút)
* **Thao tác màn hình (Thành viên 5):**
  1. Mở cửa sổ Terminal (VS Code hoặc PowerShell).
  2. Gõ lệnh: `dotnet test backend/SportsCourtBooking.sln`
  3. Kết quả chạy hiện tại: **Passed: 107, Failed: 0, Skipped: 2, Total: 109** (ngày 20/11/2026); hai test PostgreSQL được báo skipped ở lần chạy mặc định và đã chạy riêng trên database cô lập: **2 pass, 0 fail**. Chạy lại trước buổi demo nếu bộ test thay đổi.
  4. Mở file [BookingSchemaExtensions.cs](backend/src/SportsCourtBooking.Infrastructure/Data/BookingSchemaExtensions.cs) hoặc [BookingService.cs](backend/src/SportsCourtBooking.Infrastructure/Services/BookingService.cs).
* **Lời thoại gợi ý (Thành viên 5):**
  > *"Điểm sáng kỹ thuật quan trọng nhất của bài là **giải pháp chống đặt trùng giờ (Race Condition)**. Nhóm triển khai cơ chế phòng thủ 2 lớp:*\
  > *1. **Ở tầng Transaction**: Dùng `pg_advisory_xact_lock` theo mã sân và ngày để xếp hàng các yêu cầu gửi đến cùng lúc.*\
  > *2. **Ở tầng Database Constraint**: Dùng `EXCLUDE USING gist` với kiểu dữ liệu `tsrange` khoảng nửa mở `[)`. Dù có 2 request vượt qua được tầng ứng dụng thì CSDL PostgreSQL vẫn sẽ từ chối đơn thứ hai để bảo toàn tính toàn vẹn dữ liệu.*\
  > *Về độ tin cậy, nhóm có **107 test tự động (unit và service với EF InMemory)** bằng **xUnit**, tập trung vào quy tắc đặt sân, tính giá, thời gian và băm mật khẩu. Nhóm cũng có hai test tích hợp PostgreSQL cho luồng đặt đồng thời và hết hạn, có thể bật bằng biến môi trường khi chạy trong môi trường database test.*
  > *Nhóm 9 đã hoàn thành tốt mục tiêu đề ra cho học phần C#. Chúng em xin chân thành cảm ơn Thầy và rất mong nhận được những góp ý từ Thầy ạ!"*

---

## 3. Checklist Chuẩn bị Kỹ thuật Trước Giờ G

- [ ] **Mở sẵn Docker Desktop:** Đảm bảo cả 3 container (`web`, `db`, `pgadmin`) đều đang ở trạng thái `Running` (xanh lá).
- [ ] **Mở sẵn trình duyệt:**
  - Tab 1: `http://localhost:5101` (chưa đăng nhập, sẵn sàng demo).
  - Tab 2: `http://localhost:5050` (pgAdmin - phòng khi thầy muốn xem dữ liệu trong bảng CSDL).
- [ ] **Mở sẵn Terminal tại thư mục dự án:** Sẵn sàng gõ lệnh `dotnet test backend/SportsCourtBooking.sln` khi thầy muốn xem test case.
- [ ] **Mật khẩu & Tài khoản nhớ nằm lòng:**
  - Admin: `admin@sportscourt.vn` / `Admin@123`
  - Khách: `khachhang@gmail.com` / `Customer@123`
  - Chủ sân: `chusan@sportscourt.vn` / `Owner@123` → `/Owner`
- [ ] **Mạng Internet:** Toàn bộ hệ thống chạy qua Docker Cục bộ (`localhost`) nên các luồng nghiệp vụ chính không cần Internet; chuẩn bị sẵn Docker image và kiểm tra tài nguyên ngoài trước buổi demo.

---

## 4. Các Tình huống Dự phòng (Troubleshooting / Fallback)

| Tình huống xảy ra | Nguyên nhân | Cách xử lý tức thì |
| :--- | :--- | :--- |
| **Không mở được `localhost:5101`** | Container web bị dừng hoặc port bị chiếm. | Mở terminal gõ: `docker compose up -d` và kiểm tra `docker ps`. |
| **Đăng nhập báo sai tài khoản** | Nhập nhầm email hoặc mật khẩu. | Nhớ mật khẩu chuẩn có chữ hoa và ký tự đặc biệt: `Admin@123` và `Customer@123`. |
| **Thầy yêu cầu xem code xử lý CSDL** | Thầy muốn kiểm tra Entity Framework / SQL. | Mở file `BookingService.cs` (xem logic đặt) hoặc `AppDbContext.cs` (xem cấu hình quan hệ bảng). |
| **Thầy hỏi sao không thấy có thanh toán tiền thật** | Ngoài phạm vi MVP lát cắt kiến trúc. | Trả lời theo mẫu ở Phần 1: MVP tập trung giải quyết bài toán cốt lõi là giữ chỗ 10 phút và chống trùng slot; bảng `Payments` đã sẵn sàng để tích hợp cổng thanh toán ở pha sau. |

---

## 5. Hướng dẫn Trả lời Câu hỏi Kiểm soát Code (Control Code Q&A)

Nội dung dưới đây được nối từ `docs/CodeReview_QnA.md`, đối chiếu với source ngày **20/11/2026 (UTC+7)**. Đây là phần ôn phản biện; mở đúng file khi giải thích với Thầy.

### Các hằng số thời gian cần nhớ

| Nội dung | Giá trị và cách trả lời | Vị trí code |
| :--- | :--- | :--- |
| Cookie Session | **8 giờ**, `ExpireTimeSpan = TimeSpan.FromHours(8)`; có `SlidingExpiration = true`, nên phiên có thể được gia hạn khi tiếp tục sử dụng. Không phải ASP.NET Session lưu ở server. | `backend/src/SportsCourtBooking.Web/Program.cs` |
| Pending booking | **10 phút**, `Booking.PendingExpirationMinutes`; lưu `CreatedAt`/`ExpiresAt` theo UTC. `ExpiresAt = CreatedAt + 10 phút`; đơn được chủ sân xác nhận trước hạn sẽ bỏ `ExpiresAt`. | `backend/src/SportsCourtBooking.Domain/Entities/Booking.cs`, `backend/src/SportsCourtBooking.Infrastructure/Services/BookingService.cs` |
| Worker quét | **15 giây**, `PeriodicTimer(ScanInterval)`; tạo DI scope riêng mỗi lượt. Có quét ngay khi worker khởi động; không đồng nghĩa đơn luôn đổi trạng thái chính xác ở giây hết hạn. | `backend/src/SportsCourtBooking.Web/Services/PendingBookingExpirationWorker.cs` |
| Giờ mở/đóng cửa | **06:00–22:00**; khung cuối là **21:00–22:00**, tối đa **4 giờ liên tiếp**. Đây là giới hạn chung trong `BookingRules`; giờ riêng trên hồ sơ cơ sở chưa được áp dụng đầy đủ vào lịch đặt. | `backend/src/SportsCourtBooking.Application/Rules/BookingRules.cs` |
| Khung giờ vàng | Mặc định **từ 17:00**, phụ thu **50.000₫/giờ**. Có cấu hình `PeakStartHour` và `PeakSurcharge` riêng từng sân; không khẳng định mọi sân đều luôn dùng mặc định. Ví dụ sân 80.000₫/giờ: 16:00–18:00 = 80.000 + 130.000 = **210.000₫**. | `BookingRules.cs`, `backend/src/SportsCourtBooking.Domain/Entities/Court.cs`, `BookingService.cs` |
| PostgreSQL Advisory lock timeout | **10 giây**, `SET LOCAL lock_timeout = '10s'`, rồi `pg_advisory_xact_lock(hashtext(lockKey))` theo sân/ngày. Đây là thời gian tối đa chờ khóa trong transaction, không phải TTL giữ chỗ hay thời hạn chạy toàn bộ request. Khóa được nhả khi commit/rollback. | `backend/src/SportsCourtBooking.Infrastructure/Services/BookingService.cs` |
| Múi giờ | **UTC+7** qua `VietnamClock` cho ngày/giờ chơi; UTC cho thời điểm tạo đơn, hết hạn, check-in. `BookingDate`/`StartTime`/`EndTime` là ngày/giờ địa phương, không tự cộng thêm 7 giờ vào các trường này. | `backend/src/SportsCourtBooking.Application/Common/VietnamClock.cs` |

**Số test đã kiểm chứng:** `dotnet test backend/SportsCourtBooking.sln` đạt **107 pass, 0 fail, 2 skipped, tổng 109**. Hai test PostgreSQL đã được kiểm chứng riêng trên database cô lập: **2 pass, 0 fail**. Chúng chỉ chạy khi bật `RUN_POSTGRES_INTEGRATION_TESTS=1` và cung cấp `SPORTS_COURT_TEST_CONNECTION` trỏ tới database test riêng. Không tính hai test chưa chạy là pass, không chạy chúng trên database demo đang phục vụ: phần chuẩn bị schema hiện có thao tác dọn dữ liệu.

**Demo Chủ sân:** đăng nhập `chusan@sportscourt.vn` / `Owner@123`, bấm **Trang Chủ Sân** trên navbar hoặc mở `/Owner` (`/Owner/Index`). Seed đã có `CourtOwner` với ID 3 trong `AppDbContext.SeedData`; `Program.cs` bổ sung tài khoản theo email nếu còn thiếu khi khởi động. Không cần tạo lại tài khoản hoặc khởi động lại Docker để demo nếu đăng nhập đã thành công.

**Lưu ý khi demo:** dashboard `/Admin` trên web đã được gỡ; thao tác của quản trị viên thực hiện trong ứng dụng WinForms.

### 5.1. Nhóm câu hỏi về Ngôn ngữ C# 12 & Nền tảng .NET 8

#### ❓ Câu 1: Dự án sử dụng những tính năng mới nào của C# hiện đại (C# 10, 11, 12)?
* **Trả lời:**
  1. **File-scoped Namespaces (`namespace SportsCourtBooking.Web.Controllers;`):** Loại bỏ bớt một cấp thụt lề ngoặc nhọn `{}` cho toàn bộ file, giúp code sạch và dễ đọc hơn.
  2. **Kiểu dữ liệu `DateOnly` và `TimeOnly` (.NET 6/8):**
     - Thay vì dùng `DateTime` cồng kềnh và dễ sai lệch múi giờ, thực thể `Booking` dùng `DateOnly BookingDate` (chỉ lưu ngày) và `TimeOnly StartTime/EndTime` (chỉ lưu giờ).
  3. **Raw String Literals (`"""..."""` trong C# 11):**
     - Dùng trong file [BookingSchemaExtensions.cs](backend/src/SportsCourtBooking.Infrastructure/Data/BookingSchemaExtensions.cs) để viết chuỗi SQL dài nhiều dòng tự nhiên, không cần nối chuỗi `+` hay escape dấu nháy kép `\"`.
  4. **Switch Expressions & Pattern Matching:**
     - Dùng trong `HomeController.cs` và `BookingRules.cs` để ánh xạ dữ liệu ngắn gọn:
       ```csharp
       District = c.Id switch { 1 => "Quận 7", 2 => "Bình Thạnh", 3 => "Thủ Đức", _ => "Quận 10" };
       ```
  5. **Bulk Update API của EF Core (từ EF Core 7, sử dụng trong EF Core 8) (`ExecuteUpdateAsync`):**
     - Chạy trực tiếp lệnh `UPDATE` ở tầng CSDL trong Background Worker mà không cần tải dữ liệu lên bộ nhớ RAM.

---

### 5.2. Nhóm câu hỏi về Dependency Injection (DI) & Vòng đời Service

#### ❓ Câu 2: Trong `Program.cs`, tại sao lại dùng các Lifetime khác nhau (Singleton, Scoped, HostedService)?
* **Vị trí code:** [backend/src/SportsCourtBooking.Web/Program.cs](backend/src/SportsCourtBooking.Web/Program.cs)
* **Trả lời:**
  * **`AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>()`:**
    - Lớp này hoàn toàn **không lưu trạng thái (stateless)**, chỉ thực hiện thuật toán tính toán băm mật khẩu. Dùng `Singleton` để ứng dụng chỉ tạo đúng 1 đối tượng duy nhất xuyên suốt thời gian chạy, giúp tiết kiệm bộ nhớ RAM.
  * **`AddScoped<IBookingService, BookingService>()` / `AppDbContext`:**
    - Mỗi khi có 1 HTTP Request từ trình duyệt gửi tới, DI Container sẽ tạo một instance riêng cho request đó và tự động giải phóng (Dispose) khi request kết thúc.
    - **Lý do bắt buộc:** `DbContext` của Entity Framework **không an toàn theo luồng (not thread-safe)**, nếu đăng ký Singleton sẽ xảy ra xung đột dữ liệu giữa các người dùng cùng truy cập.
  * **`AddHostedService<PendingBookingExpirationWorker>()`:**
    - Đăng ký một tiến trình nền chạy độc lập với các HTTP Request, khởi động cùng ứng dụng web để thực thi tác vụ quét dọn đơn hàng định kỳ.

#### ❓ Câu 3 (Câu hỏi bẫy): *"Worker chạy ngầm là Singleton, tại sao nó có thể gọi được `PendingBookingExpirationService` vốn là Scoped?"*
* **Vị trí code:** [PendingBookingExpirationWorker.cs](backend/src/SportsCourtBooking.Web/Services/PendingBookingExpirationWorker.cs)
* **Trả lời:**
  > *"Dạ thưa Thầy, đây là lỗi **Captive Dependency** kinh điển nếu inject trực tiếp Scoped service vào Singleton Worker.*
  > *Nhóm đã xử lý chuẩn mực bằng cách: Worker chỉ nhận vào `IServiceScopeFactory`. Trong mỗi chu kỳ 15 giây quét dọn, Worker chủ động tạo một Scope con bằng lệnh:*
  > `await using var scope = _scopeFactory.CreateAsyncScope();`
  > *Sau đó lấy `PendingBookingExpirationService` ra từ Scope đó. Khi thực thi xong chu kỳ, Scope con cùng `DbContext` sẽ được tự động giải phóng sạch sẽ khỏi bộ nhớ."*

---

### 5.3. Nhóm câu hỏi về Cơ sở dữ liệu, Concurrency & ACID

#### ❓ Câu 4: Quá trình tạo đơn trong `CreateBookingAsync` đảm bảo tính toàn vẹn (ACID) như thế nào?
* **Vị trí code:** [BookingService.cs](backend/src/SportsCourtBooking.Infrastructure/Services/BookingService.cs)
* **Trả lời:**
  * **Atomicity (Nguyên tử):** Toàn bộ thao tác lấy khóa Advisory Lock, kiểm tra giờ trống, tính tiền, và lưu `Booking` mới đều nằm trong một giao dịch `await using var transaction = await _context.Database.BeginTransactionAsync()`. Nếu có bất kỳ lỗi nào xảy ra (hoặc vi phạm trùng giờ), toàn bộ thao tác sẽ bị Rollback về trạng thái ban đầu.
  * **Consistency (Nhất quán):** Ràng buộc `EXCLUDE USING gist` ở tầng CSDL đảm bảo không bao giờ có 2 đơn hàng có dải thời gian chồng lấn nhau trên cùng một sân.
  * **Isolation (Cô lập):** Dùng `pg_advisory_xact_lock` theo mã sân và ngày để cô lập hoàn toàn các luồng đặt sân cạnh tranh trên cùng một sân.
  * **Durability (Bền vững):** Khi `transaction.CommitAsync()` thành công, dữ liệu được ghi xuống đĩa cứng của PostgreSQL trong Docker volume `pgdata`.

#### ❓ Câu 5: Tại sao nhóm dùng Khóa bi quan (Pessimistic Advisory Lock) thay vì Khóa lạc quan (Optimistic Concurrency / RowVersion)?
* **Trả lời:**
  > *"Dạ thưa Thầy, Khóa lạc quan (`RowVersion` / `ConcurrencyToken`) thường áp dụng cho trường hợp nhiều người cùng sửa một bản ghi có sẵn (ví dụ sửa thông tin User).*
  > *Còn trong bài toán đặt sân, mỗi lần khách đặt là hệ thống **INSERT một bản ghi mới** vào bảng `Bookings`, chứ không phải UPDATE bản ghi cũ. Lúc này, Khóa lạc quan không thể phát hiện xung đột giữa hai câu lệnh INSERT độc lập.*
  > *Vì vậy, Khóa bi quan (`pg_advisory_xact_lock`) theo cặp `(CourtId, BookingDate)` kết hợp với **PostgreSQL Exclusion Constraint** là giải pháp chuẩn xác và triệt để nhất."*

---

### 5.4. Nhóm câu hỏi về Bảo mật (Security) & Chống Tấn công

#### ❓ Câu 6: Nếu ai đó dùng Postman/Curl cố tình gửi trực tiếp request POST đặt sân thì hệ thống có bị hack không?
* **Trả lời:**
  * **Lớp 1: Anti-CSRF Token (`[ValidateAntiForgeryToken]`):**
    - Request từ công cụ bên ngoài không có mã Token được sinh ra từ phiên đăng nhập hợp lệ sẽ bị máy chủ từ chối ngay lập tức (lỗi HTTP 400 Bad Request).
  * **Lớp 2: Kiểm tra xác thực (`[Authorize]`):**
    - Nếu không có Cookie phiên hợp lệ, server trả về mã lỗi HTTP 401 Unauthorized / chuyển hướng về trang Login.
  * **Lớp 3: Bỏ qua `UserId` do client gửi lên:**
    - Trong [BookingController.cs](backend/src/SportsCourtBooking.Web/Controllers/BookingController.cs), `UserId` của người đặt luôn được lấy trực tiếp từ **Claims của Cookie đăng nhập** (`User.GetUserId()`), hacker không thể mạo danh ID của người khác.
  * **Lớp 4: Server-side Business Rules Validation:**
    - Cho dù client can thiệp mã JavaScript để chọn 10 khung giờ hoặc chọn giờ âm, hàm `BookingRules.TryNormalizeHours` và `BookingRules.ValidateTimeRange` ở server sẽ kiểm tra lại toàn bộ và chặn ngay lập tức.

#### ❓ Câu 7: Thuật toán PBKDF2 băm mật khẩu hoạt động như thế nào?
* **Vị trí code:** [Pbkdf2PasswordHasher.cs](backend/src/SportsCourtBooking.Infrastructure/Security/Pbkdf2PasswordHasher.cs)
* **Trả lời:**
  * Mỗi mật khẩu khi tạo tài khoản sẽ được sinh một chuỗi **Salt ngẫu nhiên 16 bytes (128-bit)** bằng `RandomNumberGenerator`.
  * Sau đó đưa qua hàm băm `Rfc2898DeriveBytes` kết hợp thuật toán **HMAC-SHA256** và lặp **600.000 vòng (iterations)** để tạo ra khóa băm 256-bit.
  * Chuỗi lưu trong CSDL có định dạng: `PBKDF2$iterations$salt_base64$hash_base64`.
  * Khi người dùng đăng nhập, hệ thống tách Salt cũ ra, băm lại mật khẩu nhập vào theo số vòng được lưu trong hash (hash mới: 600.000 vòng) và so sánh bằng thuật toán so sánh an toàn `CryptographicOperations.FixedTimeEquals` để chống tấn công phân tích thời gian (Timing Attacks).
