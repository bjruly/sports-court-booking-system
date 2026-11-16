# Bộ Câu hỏi Phản biện & Soi Code Nâng cao (Code Review Q&A)
### Môn học: Ngôn ngữ Lập trình C# (.NET 8)
**Giảng viên hướng dẫn:** TS. Đỗ Như Tài | **Nhóm:** Nhóm 9

---

## 1. Nhóm câu hỏi về Ngôn ngữ C# 12 & Nền tảng .NET 8

### ❓ Câu 1: Dự án sử dụng những tính năng mới nào của C# hiện đại (C# 10, 11, 12)?
* **Trả lời:**
  1. **File-scoped Namespaces (`namespace SportsCourtBooking.Web.Controllers;`):** Loại bỏ bớt một cấp thụt lề ngoặc nhọn `{}` cho toàn bộ file, giúp code sạch và dễ đọc hơn.
  2. **Kiểu dữ liệu `DateOnly` và `TimeOnly` (.NET 6/8):** 
     - Thay vì dùng `DateTime` cồng kềnh và dễ sai lệch múi giờ, thực thể `Booking` dùng `DateOnly BookingDate` (chỉ lưu ngày) và `TimeOnly StartTime/EndTime` (chỉ lưu giờ).
  3. **Raw String Literals (`"""..."""` trong C# 11):**
     - Dùng trong file [BookingSchemaExtensions.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Infrastructure/Data/BookingSchemaExtensions.cs#L7) để viết chuỗi SQL dài nhiều dòng tự nhiên, không cần nối chuỗi `+` hay escape dấu nháy kép `\"`.
  4. **Switch Expressions & Pattern Matching:**
     - Dùng trong `HomeController.cs` và `BookingRules.cs` để ánh xạ dữ liệu ngắn gọn:
       ```csharp
       District = c.Id switch { 1 => "Quận 7", 2 => "Bình Thạnh", 3 => "Thủ Đức", _ => "Quận 10" };
       ```
  5. **Bulk Update API mới của EF Core 8 (`ExecuteUpdateAsync`):**
     - Chạy trực tiếp lệnh `UPDATE` ở tầng CSDL trong Background Worker mà không cần tải dữ liệu lên bộ nhớ RAM.

---

## 2. Nhóm câu hỏi về Dependency Injection (DI) & Vòng đời Service

### ❓ Câu 2: Trong `Program.cs`, tại sao lại dùng các Lifetime khác nhau (Singleton, Scoped, HostedService)?
* **Vị trí code:** [backend/src/SportsCourtBooking.Web/Program.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Web/Program.cs#L19-L24)
* **Trả lời:**
  * **`AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>()`:**
    - Lớp này hoàn toàn **không lưu trạng thái (stateless)**, chỉ thực hiện thuật toán tính toán băm mật khẩu. Dùng `Singleton` để ứng dụng chỉ tạo đúng 1 đối tượng duy nhất xuyên suốt thời gian chạy, giúp tiết kiệm bộ nhớ RAM.
  * **`AddScoped<IBookingService, BookingService>()` / `AppDbContext`:**
    - Mỗi khi có 1 HTTP Request từ trình duyệt gửi tới, DI Container sẽ tạo một instance riêng cho request đó và tự động giải phóng (Dispose) khi request kết thúc.
    - **Lý do bắt buộc:** `DbContext` của Entity Framework **không an toàn theo luồng (not thread-safe)**, nếu đăng ký Singleton sẽ xảy ra xung đột dữ liệu giữa các người dùng cùng truy cập.
  * **`AddHostedService<PendingBookingExpirationWorker>()`:**
    - Đăng ký một tiến trình nền chạy độc lập với các HTTP Request, khởi động cùng ứng dụng web để thực thi tác vụ quét dọn đơn hàng định kỳ.

### ❓ Câu 3 (Câu hỏi bẫy): *"Worker chạy ngầm là Singleton, tại sao nó có thể gọi được `PendingBookingExpirationService` vốn là Scoped?"*
* **Vị trí code:** [PendingBookingExpirationWorker.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Web/Services/PendingBookingExpirationWorker.cs#L32-L34)
* **Trả lời:**
  > *"Dạ thưa Thầy, đây là lỗi **Captive Dependency** kinh điển nếu inject trực tiếp Scoped service vào Singleton Worker.*  
  > *Nhóm đã xử lý chuẩn mực bằng cách: Worker chỉ nhận vào `IServiceScopeFactory`. Trong mỗi chu kỳ 15 giây quét dọn, Worker chủ động tạo một Scope con bằng lệnh:*  
  > `await using var scope = _scopeFactory.CreateAsyncScope();`  
  > *Sau đó lấy `PendingBookingExpirationService` ra từ Scope đó. Khi thực thi xong chu kỳ, Scope con cùng `DbContext` sẽ được tự động giải phóng sạch sẽ khỏi bộ nhớ."*

---

## 3. Nhóm câu hỏi về Cơ sở dữ liệu, Concurrency & ACID

### ❓ Câu 4: Quá trình tạo đơn trong `CreateBookingAsync` đảm bảo tính toàn vẹn (ACID) như thế nào?
* **Vị trí code:** [BookingService.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Infrastructure/Services/BookingService.cs#L117-L153)
* **Trả lời:**
  * **Atomicity (Nguyên tử):** Toàn bộ thao tác lấy khóa Advisory Lock, kiểm tra giờ trống, tính tiền, và lưu `Booking` mới đều nằm trong một giao dịch `await using var transaction = await _context.Database.BeginTransactionAsync()`. Nếu có bất kỳ lỗi nào xảy ra (hoặc vi phạm trùng giờ), toàn bộ thao tác sẽ bị Rollback về trạng thái ban đầu.
  * **Consistency (Nhất quán):** Ràng buộc `EXCLUDE USING gist` ở tầng CSDL đảm bảo không bao giờ có 2 đơn hàng có dải thời gian chồng lấn nhau trên cùng một sân.
  * **Isolation (Cô lập):** Dùng `pg_advisory_xact_lock` theo mã sân và ngày để cô lập hoàn toàn các luồng đặt sân cạnh tranh trên cùng một sân.
  * **Durability (Bền vững):** Khi `transaction.CommitAsync()` thành công, dữ liệu được ghi xuống đĩa cứng của PostgreSQL trong Docker volume `pgdata`.

### ❓ Câu 5: Tại sao nhóm dùng Khóa bi quan (Pessimistic Advisory Lock) thay vì Khóa lạc quan (Optimistic Concurrency / RowVersion)?
* **Trả lời:**
  > *"Dạ thưa Thầy, Khóa lạc quan (`RowVersion` / `ConcurrencyToken`) thường áp dụng cho trường hợp nhiều người cùng sửa một bản ghi có sẵn (ví dụ sửa thông tin User).*  
  > *Còn trong bài toán đặt sân, mỗi lần khách đặt là hệ thống **INSERT một bản ghi mới** vào bảng `Bookings`, chứ không phải UPDATE bản ghi cũ. Lúc này, Khóa lạc quan không thể phát hiện xung đột giữa hai câu lệnh INSERT độc lập.*  
  > *Vì vậy, Khóa bi quan (`pg_advisory_xact_lock`) theo cặp `(CourtId, BookingDate)` kết hợp với **PostgreSQL Exclusion Constraint** là giải pháp chuẩn xác và triệt để nhất."*

---

## 4. Nhóm câu hỏi về Bảo mật (Security) & Chống Tấn công

### ❓ Câu 6: Nếu ai đó dùng Postman/Curl cố tình gửi trực tiếp request POST đặt sân thì hệ thống có bị hack không?
* **Trả lời:**
  * **Lớp 1: Anti-CSRF Token (`[ValidateAntiForgeryToken]`):**
    - Request từ công cụ bên ngoài không có mã Token được sinh ra từ phiên đăng nhập hợp lệ sẽ bị máy chủ từ chối ngay lập tức (lỗi HTTP 400 Bad Request).
  * **Lớp 2: Kiểm tra xác thực (`[Authorize]`):**
    - Nếu không có Cookie phiên hợp lệ, server trả về mã lỗi HTTP 401 Unauthorized / chuyển hướng về trang Login.
  * **Lớp 3: Bỏ qua `UserId` do client gửi lên:**
    - Trong [BookingController.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Web/Controllers/BookingController.cs#L64-L70), `UserId` của người đặt luôn được lấy trực tiếp từ **Claims của Cookie đăng nhập** (`User.GetUserId()`), hacker không thể mạo danh ID của người khác.
  * **Lớp 4: Server-side Business Rules Validation:**
    - Cho dù client can thiệp mã JavaScript để chọn 10 khung giờ hoặc chọn giờ âm, hàm `BookingRules.TryNormalizeHours` và `BookingRules.ValidateTimeRange` ở server sẽ kiểm tra lại toàn bộ và chặn ngay lập tức.

### ❓ Câu 7: Thuật toán PBKDF2 băm mật khẩu hoạt động như thế nào?
* **Vị trí code:** [Pbkdf2PasswordHasher.cs](file:///c:/Users/ASUS%20TUF/Documents/GitHub/sports-court-booking-system/backend/src/SportsCourtBooking.Infrastructure/Security/Pbkdf2PasswordHasher.cs#L14-L40)
* **Trả lời:**
  * Mỗi mật khẩu khi tạo tài khoản sẽ được sinh một chuỗi **Salt ngẫu nhiên 16 bytes (128-bit)** bằng `RandomNumberGenerator`.
  * Sau đó đưa qua hàm băm `Rfc2898DeriveBytes` kết hợp thuật toán **HMAC-SHA256** và lặp **100.000 vòng (iterations)** để tạo ra khóa băm 256-bit.
  * Chuỗi lưu trong CSDL có định dạng: `{Salt dạng Base64}:{Hash dạng Base64}`.
  * Khi người dùng đăng nhập, hệ thống tách Salt cũ ra, băm lại mật khẩu nhập vào đúng 100.000 lần và so sánh bằng thuật toán so sánh an toàn `CryptographicOperations.FixedTimeEquals` để chống tấn công phân tích thời gian (Timing Attacks).
