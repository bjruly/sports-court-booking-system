# Kịch bản Demo & Thuyết trình Đồ án C# (Demo Script)
### Đề tài: Hệ thống Đặt Sân Thể Thao Trực Tuyến (Sân Nhà)

> **Giảng viên hướng dẫn:** TS. Đỗ Như Tài  
> **Nhóm thực hiện:** Nhóm 9 (5 thành viên)  
> • 3124411038 - Trần Lê Gia Bảo  
> • 3124411245 - Huỳnh Nhật Quang  
> • 3124411337 - Nguyễn Thanh Tuấn  
> • 3124411261 - Đào Hữu Tài  
> • 3124411198 - Tô Thành Nhân  
> **Thời lượng demo chuẩn:** 6 – 8 phút (cộng thời gian Q&A phản biện)

---

## 1. Phân chia Vai trò 5 Thành viên

| Thành viên | Nhiệm vụ chính trong buổi demo | Nội dung phụ trách |
| :--- | :--- | :--- |
| **Thành viên 1** (Trưởng nhóm) | **Mở đầu & Giới thiệu Kiến trúc** | Giới thiệu thành viên, mục tiêu đề tài, kiến trúc Clean Architecture / Modular Monolith. |
| **Thành viên 2** | **Demo Luồng Khách hàng (User Flow)** | Trực tiếp thao tác: Tìm sân, lọc theo môn/ngày, xem bảng giá và lịch trống. |
| **Thành viên 3** | **Demo Đặt sân & Quy tắc nghiệp vụ** | Chọn khung giờ, giải thích quy tắc giờ cao điểm, giới hạn 4 giờ, tạo đơn `Pending`. |
| **Thành viên 4** | **Demo Luồng Quản trị (Admin Flow)** | Đăng nhập Admin, trang Quản lý lịch đặt, lọc theo ngày/sân, cơ chế Background Worker 10 phút. |
| **Thành viên 5** | **Giải pháp Kỹ thuật & Kết quả Test** | Thuyết trình Concurrency (Advisory Lock, GiST Constraint), 49 bài Unit Tests, Docker, kết luận. |

*(Nếu nhóm phân công 1 người nói và 1 người thao tác máy chiếu, có thể linh hoạt theo kịch bản dưới đây).*

---

## 2. Kịch bản Chi tiết Từng Bước (Kèm Lời thoại & Thao tác)

### ⏱️ Phần 1: Mở đầu & Tổng quan Công nghệ (1 phút)
* **Thao tác màn hình:** Chiếu slide trang bìa hoặc mở trang chủ `http://localhost:5101`.
* **Lời thoại gợi ý (Thành viên 1):**
  > *"Kính thưa Thầy Đỗ Như Tài, hôm nay Nhóm 9 xin đại diện báo cáo và demo đồ án môn học Ngôn ngữ lập trình C# với đề tài: **Hệ thống đặt sân thể thao trực tuyến - Sân Nhà**.*  
  > *Dự án của nhóm được xây dựng trên nền tảng **.NET 8** và ngôn ngữ **C# 12**, áp dụng mô hình **Modular Monolith** kết hợp nguyên lý **Clean Architecture** tách biệt 4 project: Domain, Application, Infrastructure và Web.*  
  > *Hệ thống sử dụng **PostgreSQL 16** làm cơ sở dữ liệu quan hệ, kết nối qua **Entity Framework Core 8**, và toàn bộ ứng dụng được container hóa đồng nhất bằng **Docker Compose**."*

---

### ⏱️ Phần 2: Luồng Khách hàng - Tìm kiếm & Xem Lịch trống (1.5 phút)
* **Thao tác màn hình (Thành viên 2):**
  1. Mở trang chủ: `http://localhost:5101`
  2. Tại thanh tìm kiếm, chọn môn **Cầu lông**, chọn ngày chơi là **ngày mai** (hoặc ngày bất kỳ phía trước), bấm **Tìm sân**.
  3. Chỉ vào danh sách kết quả (hiện ra các sân cầu lông VIP).
  4. Bấm vào thẻ **"Sân Cầu Lông VIP 1"** để chuyển đến trang xem lịch (`/Home/Schedule/1`).
* **Lời thoại gợi ý (Thành viên 2):**
  > *"Trước hết, em xin phép demo trải nghiệm của Khách hàng tìm sân. Ngay tại trang chủ, khách hàng có thể lọc nhanh theo bộ môn thể thao và ngày muốn chơi.*  
  > *Khi bấm vào một sân cụ thể, hệ thống sẽ tải bảng lưới lịch biểu từ 06:00 sáng đến 22:00 đêm.*  
  > *Tại đây có 2 điểm nghiệp vụ quan trọng được nhóm cài đặt:*  
  > *1. **Chặn giờ quá khứ:** Hệ thống tự động so sánh với giờ hiện tại của Việt Nam (`VietnamClock`); những khung giờ đã trôi qua sẽ bị khóa và hiển thị trạng thái 'Đã qua'.*  
  > *2. **Bảng giá động theo khung giờ cao điểm:** Từ 17:00 trở đi, hệ thống tự động cộng thêm phụ thu 50.000₫/giờ theo quy định, hiển thị trực quan giá từng ô để người chơi nắm rõ trước khi đặt."*

---

### ⏱️ Phần 3: Đặt sân, Review Chi phí & Tạo Đơn (2 phút)
* **Thao tác màn hình (Thành viên 3):**
  1. Thử cố tình chọn 5 ô liên tiếp $\rightarrow$ Hệ thống hiện thông báo lỗi: *"Mỗi lượt đặt tối đa 4 khung giờ liên tiếp"*.
  2. Chọn đúng 2 khung giờ liên tiếp (ví dụ: `18:00 – 19:00` và `19:00 – 20:00`).
  3. Bấm nút **"Tiếp tục đặt sân"** $\rightarrow$ Hệ thống chuyển sang trang **Review** (`/Home/Review`).
  4. Bấm **"Xác nhận đặt sân"**:
     - Do chưa đăng nhập, hệ thống tự chuyển sang trang Đăng nhập (`/Home/Login?returnUrl=...`).
     - Đăng nhập tài khoản khách: `khachhang@gmail.com` / `Customer@123`.
     - Sau khi đăng nhập thành công, hệ thống tự động giữ nguyên lựa chọn sân và chuyển đến trang **"Lịch đặt của tôi" (`/Booking/My`)**.
* **Lời thoại gợi ý (Thành viên 3):**
  > *"Tiếp theo là quy trình đặt sân và kiểm tra tính hợp lệ:*  
  > *Nhóm cài đặt quy tắc nghiệp vụ tập trung ở `BookingRules.cs`: người dùng chỉ được đặt tối đa 4 khung giờ liên tiếp. Nếu cố tình chọn sai, hệ thống sẽ chặn ngay từ giao diện lẫn ở tầng Server Controller.*  
  > *Tại trang Xác nhận đặt sân, người dùng xem lại chi tiết sân, ngày, khung giờ và tổng tiền được tính tự động.*  
  > *Hệ thống bắt buộc xác thực khi tạo đơn. Với cơ chế `returnUrl` an toàn, sau khi đăng nhập xong khách hàng lập tức hoàn tất đơn đặt và được chuyển về trang 'Lịch đặt của tôi'. Đơn hàng vừa tạo lập tức mang trạng thái **Pending (Chờ xác nhận)**."*

---

### ⏱️ Phần 4: Góc nhìn Quản trị & Cơ chế Giữ chỗ 10 phút (1.5 phút)
* **Thao tác màn hình (Thành viên 4):**
  1. Bấm **Đăng xuất** ở góc phải trên.
  2. Bấm **Đăng nhập**, nhập tài khoản Admin:
     - Email: `admin@sportscourt.vn`
     - Mật khẩu: `Admin@123`
  3. Trên thanh điều hướng (navbar), bấm vào nút **"Quản lý lịch đặt"** (vừa được thêm).
  4. Trang `/Booking` hiện ra:
     - Chọn đúng ngày vừa đặt đơn ở Bước 3.
     - Danh sách hiện ra đơn đặt của khách `Nguyễn Văn Khách`, hiển thị sân, giờ, số tiền và trạng thái `Pending`.
     - Thử chọn bộ lọc sân để lọc danh sách.
* **Lời thoại gợi ý (Thành viên 4):**
  > *"Bây giờ em xin chuyển sang góc nhìn của Quản trị viên / Nhân viên.*  
  > *Khi đăng nhập bằng tài khoản Quản trị viên, thanh điều hướng sẽ hiển thị thêm mục **'Quản lý lịch đặt'** dựa trên cơ chế Role-Based Access Control (`User.IsInRole('Admin')`).*  
  > *Tại đây, nhân viên có thể tra cứu toàn bộ đơn đặt sân của khách theo từng ngày và lọc theo từng sân cụ thể.*  
  > *Về cơ chế giữ chỗ: Nhóm thiết kế đơn đặt mới có thời hạn giữ chỗ là **10 phút** (`ExpiresAt = CreatedAt + 10 phút`).*  
  > *Để tự động hóa việc này, nhóm xây dựng một **BackgroundService** mang tên `PendingBookingExpirationWorker` chạy ngầm mỗi 15 giây. Worker này sử dụng phương thức `ExecuteUpdateAsync` của EF Core 8 để tự động chuyển các đơn quá hạn sang trạng thái `Expired`, giải phóng khung giờ cho khách khác mà không cần nhân viên phải thao tác thủ công."*

---

### ⏱️ Phần 5: Điểm sáng Kỹ thuật, Unit Tests & Tổng kết (1.5 phút)
* **Thao tác màn hình (Thành viên 5):**
  1. Mở cửa sổ Terminal (VS Code hoặc PowerShell).
  2. Gõ lệnh: `dotnet test backend/SportsCourtBooking.sln`
  3. Kết quả chạy hiện tại: **Passed: 49, Failed: 0**; chạy lại trước buổi demo và cập nhật số nếu bộ test thay đổi.
  4. Mở file [BookingSchemaExtensions.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Infrastructure/Data/BookingSchemaExtensions.cs) hoặc [BookingService.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Infrastructure/Services/BookingService.cs#L125-L128).
* **Lời thoại gợi ý (Thành viên 5):**
  > *"Điểm sáng kỹ thuật quan trọng nhất của bài là **giải pháp chống đặt trùng giờ (Race Condition)**. Nhóm triển khai cơ chế phòng thủ 2 lớp:*  
  > *1. **Ở tầng Transaction**: Dùng `pg_advisory_xact_lock` theo mã sân và ngày để xếp hàng các yêu cầu gửi đến cùng lúc.*  
  > *2. **Ở tầng Database Constraint**: Dùng `EXCLUDE USING gist` với kiểu dữ liệu `tsrange` khoảng nửa mở `[)`. Dù có 2 request vượt qua được tầng ứng dụng thì CSDL PostgreSQL vẫn sẽ từ chối đơn thứ hai để bảo toàn tính toàn vẹn dữ liệu.*  
  > *Về độ tin cậy, nhóm có **49 Unit Tests** bằng **xUnit**, tập trung vào quy tắc đặt sân, tính giá, thời gian và băm mật khẩu. Ngoài unit test, cần kiểm thử tích hợp PostgreSQL cho luồng đặt đồng thời và hết hạn.*
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
- [ ] **Mạng Internet:** Toàn bộ hệ thống chạy qua Docker Cục bộ (`localhost`) nên **hoàn toàn KHÔNG phụ thuộc vào mạng WiFi của trường**, đảm bảo 100% không bị lag/mất kết nối khi demo!

---

## 4. Các Tình huống Dự phòng (Troubleshooting / Fallback)

| Tình huống xảy ra | Nguyên nhân | Cách xử lý tức thì |
| :--- | :--- | :--- |
| **Không mở được `localhost:5101`** | Container web bị dừng hoặc port bị chiếm. | Mở terminal gõ: `docker compose up -d` và kiểm tra `docker ps`. |
| **Đăng nhập báo sai tài khoản** | Nhập nhầm email hoặc mật khẩu. | Nhớ mật khẩu chuẩn có chữ hoa và ký tự đặc biệt: `Admin@123` và `Customer@123`. |
| **Thầy yêu cầu xem code xử lý CSDL** | Thầy muốn kiểm tra Entity Framework / SQL. | Mở file `BookingService.cs` (xem logic đặt) hoặc `AppDbContext.cs` (xem cấu hình quan hệ bảng). |
| **Thầy hỏi sao không thấy có thanh toán tiền thật** | Ngoài phạm vi MVP lát cắt kiến trúc. | Trả lời theo mẫu ở Phần 1: MVP tập trung giải quyết bài toán cốt lõi là giữ chỗ 10 phút và chống trùng slot; bảng `Payments` đã sẵn sàng để tích hợp cổng thanh toán ở pha sau. |
