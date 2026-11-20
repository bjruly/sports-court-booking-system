# Báo cáo đánh giá Sports Court Booking System

**Ngày đánh giá:** 20/11/2026, múi giờ UTC+7.  
**Học phần:** C# / .NET 8 MVC — **Giảng viên:** TS. Đỗ Như Tài — **Nhóm:** Nhóm 9.  
**Source tham chiếu:** commit `7d22aa1` cùng các chỉnh sửa tài liệu và test trong lần làm việc này.  
**Ứng dụng kiểm thử:** `http://localhost:5101`, Docker Compose đang chạy sẵn.

Đánh giá theo bằng chứng code, kết quả test và HTTP thật. Không có rubric chính thức của giảng viên trong yêu cầu, nên mức ưu tiên dưới đây là nhận định kỹ thuật để chuẩn bị bảo vệ, không phải điểm số được giảng viên cam kết. Hệ thống đã có nghiệp vụ cốt lõi và cơ chế chống trùng lịch tốt; các điểm dễ bị phản biện nhất nằm ở vòng đời dữ liệu khi khởi động, tính nguyên tử ngoài luồng booking và sự khác biệt giữa source với bản đang demo.

## Phạm vi và bằng chứng kiểm chứng

Đã đọc bốn file yêu cầu: [BookingService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/BookingService.cs), [OwnerController.cs](../backend/src/SportsCourtBooking.Web/Controllers/OwnerController.cs), [AdminController.cs](../backend/src/SportsCourtBooking.Web/Controllers/AdminController.cs), [AccountController.cs](../backend/src/SportsCourtBooking.Web/Controllers/AccountController.cs). Đối chiếu thêm `Program.cs`, các service liên quan, DTO/rules, cấu hình EF, schema extensions, migrations, Razor views và toàn bộ nhóm test hiện có.

| Kiểm chứng | Kết quả |
| :--- | :--- |
| `dotnet build backend/SportsCourtBooking.sln --no-restore` | **Thành công, 0 warning, 0 error**, gồm cả project Web/Razor. |
| `dotnet test backend/SportsCourtBooking.sln` | **107 pass, 0 fail, 2 skipped, tổng 109**. |
| Chạy riêng `PostgreSqlIntegrationTests` với PostgreSQL thật | **2 pass, 0 fail**, database cô lập được tạo từ migrations, chuẩn bị schema và đồng bộ identity sequence; đã xóa sau khi chạy. |
| Kiểm tra luồng HTTP trên container đang phục vụ | **23/23 kiểm tra đạt**, cộng kiểm chứng worker riêng. Chi tiết trong bảng bên dưới. |
| Coverage bằng `--collect:'XPlat Code Coverage'` | **1.575/6.267 dòng = 25,13%**, branch **35,45%** trên các assembly được đo. Application **57,79%**, Domain **73,46%**, Infrastructure **21,65%**. **Không bao gồm Web/controller** vì project test chưa tham chiếu Web. Đây không phải coverage toàn hệ thống. |
| Docker | `web`, `db`, `pgadmin` tiếp tục chạy; database healthy. Không rebuild, restart, stop container hoặc thay đổi schema của database demo. |

Bằng chứng có cấu trúc: [EvaluationEvidence.json](EvaluationEvidence.json). TRX và coverage gốc nằm trong thư mục local bị Git ignore `artifacts/evaluation-tests/`. Kiểm thử dùng HTTP thật, cookie đăng nhập và anti-forgery token được cấp từ Razor; **chưa kiểm thử render bằng trình duyệt, JavaScript, responsive hoặc thao tác chuột**.

**Chuẩn bị dữ liệu:** tạo một sân riêng có tên `Evaluation-20261120`, voucher `EVAL20261120`, hai booking Confirmed của hôm qua/hôm nay để có thể kiểm thử Review/Check-in ngay. Hai booking này là fixture chèn vào DB, không phải bằng chứng ứng dụng cho phép khách đặt trong quá khứ. Các booking còn lại được tạo qua HTTP. Sau kiểm thử đã dọn **1 sân, 4 booking, 1 review và 1 voucher** thuộc fixture; không chỉnh ngày/giờ hệ điều hành hay trạng thái booking có sẵn của người dùng.

### Kết quả các luồng web thật

| Luồng | Thao tác / dữ liệu | Kết quả quan sát |
| :--- | :--- | :--- |
| Admin login | `admin@sportscourt.vn` / `Admin@123` | Có cookie `SanNha.Auth`, chuyển `/Admin`, HTTP 200. `/Admin/Customers`, `/Admin/Reports`, `/Booking` trả 200. |
| Customer login | `khachhang@gmail.com` / `Customer@123` | Đăng nhập thành công; `/Account/Profile`, `/Account/ChangePassword`, `/Booking/My` trả 200. |
| CourtOwner login | `chusan@sportscourt.vn` / `Owner@123` | Chuyển `/Owner`, HTTP 200; navbar trang chủ có liên kết Chủ sân. |
| Các trang Owner | `/Owner/Calendar`, `/Bookings`, `/Courts`, `/Facilities`, `/Pricing`, `/Vouchers`, `/Reviews`, `/RevenueReport`, `/CourtStatistics`, `/Profile`, `/ChangePassword` dưới prefix `/Owner` | Tất cả HTTP 200, không chuyển về Login/AccessDenied. Kiểm chứng tải trang, chưa xác nhận toàn bộ thao tác CRUD của từng trang. |
| Role isolation | Customer truy cập `/Owner`, `/Admin`; Owner truy cập `/Admin` | HTTP 302 đến `/Home/AccessDenied`; chưa có cookie thì phải đăng nhập. Không coi trang AccessDenied sau redirect trả 200 là truy cập thành công. |
| Tạo voucher | Owner tạo mã giảm 25%, giới hạn 1 lượt | Thành công; xuất hiện trên danh sách voucher. |
| Phụ thu và voucher | Sân 80.000₫/giờ, 16:00–18:00 ngày 21/11/2026 | Preview **80.000 + 130.000 = 210.000₫**; giảm **52.500₫**, còn **157.500₫**. |
| Tạo booking | Customer POST `/Home/ConfirmBooking` với voucher | Tạo đúng một đơn Pending; DB lưu `TotalPrice = 157500`, `DiscountAmount = 52500`; chi tiết hiển thị đúng. |
| Giữ slot | Kiểm tra availability sau đặt | Slot không còn trống. |
| Xác nhận | Owner POST `/Owner/ConfirmBooking` | Pending → Confirmed, bỏ `ExpiresAt`. |
| Giới hạn voucher | Thử dùng lần hai khi đơn đầu vẫn Confirmed | Không sinh thêm booking; thông báo hết lượt sử dụng. |
| Customer hủy | POST `/Booking/CancelMy` cho đơn Confirmed chưa trả tiền, chưa đến giờ | Đơn Cancelled; slot trống trở lại; voucher có thể áp dụng lại trên preview. |
| Quyền xem chi tiết | Dùng phiên Admin mở chi tiết booking thuộc Customer | HTTP 404 theo điều kiện sở hữu; chức năng quản trị dùng `/Booking`. |
| Check-in đúng ngày | Owner check-in booking Confirmed fixture hôm nay | Lưu được `CheckInAt` UTC. Gửi lại bị từ chối; check-in đơn ngày mai bị từ chối. |
| Review | Booking Confirmed hôm qua, đã kết thúc | Customer gửi 5 sao thành công, sửa còn 4 sao thành công; DB chỉ có một review cho booking. Rating 6 bị validation; đơn tương lai trả 404 khi mở form review. |
| Owner phản hồi review | POST phản hồi trên `/Owner/Reviews` | Lưu được nội dung trả lời và xuất hiện trong DB. |
| CSRF | POST đặt sân đã đăng nhập nhưng bỏ token | HTTP 400. |
| Validation số giờ | Gửi 5 khung giờ liên tiếp | Không tạo booking; báo tối đa 4 khung. |
| Đặt đồng thời | Hai HTTP POST song song cùng sân/ngày/20:00–21:00 | Chỉ một booking mới; request còn lại nhận thông báo slot đã có người đặt. Đây là smoke test 2 request, chưa phải load test. |
| Worker hết hạn | Đặt `ExpiresAt` của riêng booking fixture thành UTC hiện tại + 2 giây; chỉ đọc DB sau đó | Worker của container tự chuyển Pending → Expired. Không gọi availability/booking API trong khoảng quan sát để tránh nhầm với cơ chế expire khi đọc. |
| Khác biệt bản chạy | Admin mở `/Admin/Facilities`, `/Admin/Owners`, `/Admin/Reviews` | **HTTP 404** dù source có action/view. DB đang chạy cũng chưa có `Courts.District`. Source build xanh không đồng nghĩa Docker đang chạy bản source đó. |

## ✅ Những tính năng đã hoàn thiện

Các mục dưới đây nêu rõ phần hoàn thiện ở source và bằng chứng runtime tương ứng; không suy rộng rằng mọi nhánh lỗi đều đã được test.

| Nhóm tính năng | Code / view tham chiếu | Giá trị khi bảo vệ đồ án |
| :--- | :--- | :--- |
| Kiến trúc và DI | [Program.cs](../backend/src/SportsCourtBooking.Web/Program.cs), các project Domain/Application/Infrastructure/Web | Phân tầng entity, DTO/interface/rules, persistence/service và MVC; đăng ký scoped DbContext/service, singleton password hasher và hosted worker. |
| Xác thực, phân quyền | [AuthController.cs](../backend/src/SportsCourtBooking.Web/Controllers/AuthController.cs), [AuthService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/AuthService.cs), `[Authorize(Roles = "CourtOwner")]` trên OwnerController và `Admin` trên AdminController | Login bằng cookie; lấy danh tính từ claims, kiểm tra return URL nội bộ; các POST nghiệp vụ có anti-forgery. Ba role đã đăng nhập được trên web thật. |
| Seed Chủ sân và navbar | [AppDbContext.cs](../backend/src/SportsCourtBooking.Infrastructure/Data/AppDbContext.cs) `SeedData`, [SeedCredentials.cs](../backend/src/SportsCourtBooking.Infrastructure/Data/SeedCredentials.cs), [_Layout.cshtml](../backend/src/SportsCourtBooking.Web/Views/Shared/_Layout.cshtml) | Đã có user ID 3, `CourtOwner`, email `chusan@sportscourt.vn`; navbar desktop/mobile có “Trang Chủ Sân” đến Owner/Index. Program bổ sung tài khoản theo email nếu thiếu. Không cần thêm seed/nav trùng lặp. |
| Băm mật khẩu | [Pbkdf2PasswordHasher.cs](../backend/src/SportsCourtBooking.Infrastructure/Security/Pbkdf2PasswordHasher.cs), [PasswordHasherTests.cs](../backend/tests/SportsCourtBooking.Tests/PasswordHasherTests.cs) | PBKDF2-HMAC-SHA256, salt ngẫu nhiên 16 byte, hash 32 byte, **600.000 vòng**, so sánh constant-time; thông số vòng lặp lưu cùng hash. |
| Booking và giá | `BookingService.CreateBookingCoreAsync`, [BookingRules.cs](../backend/src/SportsCourtBooking.Application/Rules/BookingRules.cs), [HomeController.cs](../backend/src/SportsCourtBooking.Web/Controllers/HomeController.cs), [Review.cshtml](../backend/src/SportsCourtBooking.Web/Views/Home/Review.cshtml) | Server kiểm tra giờ, ngày, tối đa 4 giờ, sân/cơ sở hoạt động; tính lại giá và giảm giá khi lưu, không tin tổng tiền client gửi. Luồng phụ thu/voucher đã kiểm chứng đến DB. |
| Transaction và chống trùng | `BookingService.CreateBookingCoreAsync`, [BookingSchemaExtensions.cs](../backend/src/SportsCourtBooking.Infrastructure/Data/BookingSchemaExtensions.cs) | Transaction tạo booking, advisory lock sân/ngày, `lock_timeout = '10s'`, GiST exclusion constraint theo khoảng `[)` cho Pending/Confirmed; đã có test PostgreSQL và kiểm tra HTTP đồng thời. Không được mô tả là “chưa có ACID/concurrency”. |
| Pending expiration | [Booking.cs](../backend/src/SportsCourtBooking.Domain/Entities/Booking.cs), [PendingBookingExpirationService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/PendingBookingExpirationService.cs), [PendingBookingExpirationWorker.cs](../backend/src/SportsCourtBooking.Web/Services/PendingBookingExpirationWorker.cs) | TTL 10 phút; worker 15 giây, scope riêng; SQL UPDATE có điều kiện. Tác vụ đọc/tạo booking cũng quét hết hạn. Worker runtime và test PostgreSQL đều đã kiểm chứng. |
| Owner dashboard, lịch và walk-in | `OwnerController`, [OwnerDashboardService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/OwnerDashboardService.cs), [Calendar.cshtml](../backend/src/SportsCourtBooking.Web/Views/Owner/Calendar.cshtml), [CreateWalkInBooking.cshtml](../backend/src/SportsCourtBooking.Web/Views/Owner/CreateWalkInBooking.cshtml) | Có service và Razor view; truy vấn theo OwnerId; tạo walk-in có GuestName/GuestPhone. Unit/service tests hiện có cho walk-in và phân tách chủ sân; runtime đã tải được lịch. |
| Xác nhận/hủy/check-in | `ConfirmBookingScopedAsync`, `CancelOwnBookingAsync`, `CheckInOwnerBookingAsync` trong BookingService; [Bookings.cshtml](../backend/src/SportsCourtBooking.Web/Views/Owner/Bookings.cshtml), [Details.cshtml](../backend/src/SportsCourtBooking.Web/Views/Booking/Details.cshtml) | UPDATE có điều kiện để chống chuyển trạng thái lặp; Customer hủy còn kiểm tra người sở hữu, ngày giờ, active và payment chưa Paid. Check-in lưu timestamp, chống bấm lại; đã thử web thật. |
| Voucher | [OwnerVoucherService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/OwnerVoucherService.cs), [VoucherRules.cs](../backend/src/SportsCourtBooking.Application/Rules/VoucherRules.cs), [Vouchers.cshtml](../backend/src/SportsCourtBooking.Web/Views/Owner/Vouchers.cshtml) | Có CRUD/status, mã theo chủ sân, phần trăm/số tiền, giới hạn lượt; BookingService khóa theo voucher trước khi đếm lượt. Ngày hiệu lực tính theo ngày áp dụng mã, đã ghi rõ trong view. |
| Review và trả lời | [ReviewService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/ReviewService.cs), [ReviewRules.cs](../backend/src/SportsCourtBooking.Application/Rules/ReviewRules.cs), [BookingController.cs](../backend/src/SportsCourtBooking.Web/Controllers/BookingController.cs), [Review.cshtml](../backend/src/SportsCourtBooking.Web/Views/Booking/Review.cshtml), [Reviews.cshtml](../backend/src/SportsCourtBooking.Web/Views/Owner/Reviews.cshtml) | Customer chỉ đánh giá booking của mình, Confirmed, đã kết thúc; unique BookingId, rating 1–5, nhận xét ≤500 ký tự; có khóa khi lưu review, Owner trả lời ≤1.000 ký tự. Đã thử tạo/sửa/trả lời. |
| Admin và báo cáo ở source | AdminController; [FacilityService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/FacilityService.cs), [UserService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/UserService.cs), [ReportService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/ReportService.cs), thư mục [Views/Admin](../backend/src/SportsCourtBooking.Web/Views/Admin) | Có quản lý danh mục, khách/chủ sân, cơ sở, moderation đánh giá và báo cáo. Ba trang Admin nêu trên chưa hoạt động trên image hiện tại, cần phân biệt với source đã có. |
| Hồ sơ và đổi mật khẩu | AccountController, [AccountService.cs](../backend/src/SportsCourtBooking.Infrastructure/Services/AccountService.cs), thư mục [Views/Account](../backend/src/SportsCourtBooking.Web/Views/Account) | Kiểm tra active, validation DTO, cập nhật tên claim; đổi mật khẩu có điều kiện hash cũ để tránh ghi đè. Đã tải GET trên runtime; chưa đổi mật khẩu demo trong lần kiểm thử này. |
| Persistence và Docker | [AppDbContext.cs](../backend/src/SportsCourtBooking.Infrastructure/Data/AppDbContext.cs), [migrations](../backend/src/SportsCourtBooking.Infrastructure/Data/Migrations), [docker-compose.yml](../docker-compose.yml) | Có FK, unique/index, check constraints, EF migrations, volume PostgreSQL và Data Protection keys. PostgreSQL riêng đã chạy hai integration tests thành công. |

## ⚠️ Những phần làm dở dang hoặc chưa đồng bộ

| Ưu tiên | Phát hiện và bằng chứng | Tác động / hướng hoàn thiện |
| :--- | :--- | :--- |
| **P0** | `BookingSchemaExtensions.EnsureBookingSchemaAsync`, khoảng dòng 120–128, chạy DELETE booking có `UserId = 2`, xóa user/review/cơ sở theo dữ liệu hardcode. `Program.cs` gọi hàm này khi khởi động. | Có thể làm mất lịch của tài khoản demo sau restart; khi đã có Review liên kết, DELETE booking có thể bị FK từ chối và làm startup thất bại. Tách dọn dữ liệu thành tác vụ dev chủ động; startup/schema migration phải bảo toàn dữ liệu. Không restart Docker trước khi xử lý và kiểm tra backup. Phát hiện qua source, không cố tình tái hiện bằng restart live. |
| **P0** | Source có Admin Facilities/Owners/Reviews nhưng runtime trả 404. DB live chưa có `Courts.District` và `Reviews.HiddenAt` trong model/migration hiện tại; thao tác chuẩn bị fixture bằng EF model hiện tại ban đầu bị PostgreSQL 42703 rồi rollback. | Image/schema đang chạy khác source. Fixture dùng mapping tạm bỏ các thuộc tính mới chưa có ở schema cũ để không sửa schema live. Cần deploy thử trên bản sao database trước; chỉ cập nhật container phục vụ demo khi đã kiểm tra dữ liệu không bị xóa. |
| **P1** | `OwnerFacilityService.CreateOwnerFacilityAsync` gọi `SaveChangesAsync` lưu Facility, sau đó thêm Courts và gọi SaveChanges lần hai, không có transaction chung. | Mỗi SaveChanges có transaction riêng nhưng **toàn bộ nghiệp vụ chưa nguyên tử**: lần hai lỗi sẽ để lại cơ sở không có sân. Dùng một SaveChanges với graph Facility→Courts hoặc transaction bao cả hai; test lỗi ở bước lưu sân. |
| **P1** | Có `Facility.OpenTime/CloseTime`, form cho Owner chỉnh, nhưng `BookingRules`, lịch sân và CreateBookingCore dùng giới hạn chung 06:00–22:00. | Hồ sơ cơ sở nói một giờ nhưng server vẫn cho đặt theo giờ chung. Quyết định hỗ trợ giờ riêng và kiểm tra ở server + UI, hoặc khóa cấu hình giờ riêng để đúng phạm vi. |
| **P1** | AdminController `CreateFacility`/`UpdateFacility` chưa kiểm tra `ModelState.IsValid`; FacilityService chủ yếu kiểm tra trường trống. Các service facility cũng chưa validate đầy đủ định dạng giờ/thứ tự mở–đóng và mọi giới hạn độ dài. | Dữ liệu không hợp lệ có thể đi đến DB và phát sinh lỗi thay vì hiển thị validation. Chuẩn hóa validation DTO và business validation cho tất cả entry point; thêm ca âm. |
| **P1** | `Program.cs` xác thực cookie không có `OnValidatePrincipal`/security stamp hoặc cơ chế kiểm tra lại trạng thái tài khoản chung; nhiều action Owner/Admin tin role claim đã cấp. | Suy luận từ source: khóa tài khoản hoặc đổi mật khẩu chưa thu hồi mọi cookie cũ ngay. Một số service có kiểm tra IsActive riêng, nhưng chưa phủ toàn bộ. Chưa khóa tài khoản demo để thử vì sẽ ảnh hưởng phiên sử dụng của người dùng. |
| **P1** | `PendingBookingExpirationWorker` không bao exception theo từng vòng quét. | Suy luận: lỗi DB tạm thời có thể thoát `ExecuteAsync`, làm worker dừng và có thể kéo host dừng theo cấu hình mặc định. Thêm logging/retry có giới hạn; xử lý cancellation đúng, test phục hồi sau lỗi tạm thời. |
| **P2** | Entity/report có `BookingStatus.Completed`, nhưng chưa tìm thấy action/service chuyển booking sang Completed. ReviewRules hiện chỉ chấp nhận Confirmed đã hết giờ. | Chốt state machine và ý nghĩa Completed; nếu thêm hoàn thành thì cập nhật ReviewRules cùng tests để đơn hoàn thành vẫn đánh giá được. |
| **P2** | ReviewService hỗ trợ ẩn/hiện và lọc `IsReported`; chưa tìm thấy luồng người dùng báo cáo vi phạm. Trang Home/Schedule hiện chưa đọc danh sách review công khai từ ReviewService. | Hoàn thiện endpoint/view “Báo cáo” nếu nằm trong use case; hiển thị review/điểm trung bình có lọc IsHidden. Phân biệt `/Home/Review` là **xem lại đơn**, `/Booking/Review` mới là **đánh giá**. |
| **P2** | `FacilityService.ApproveFacilityAsync` và mở khóa cơ sở đặt mọi sân thành Active; không giữ trạng thái Maintenance trước đó. | Có thể vô tình mở lại sân đang bảo trì. Chỉ kích hoạt sân phù hợp với quy trình phê duyệt; lưu/khôi phục trạng thái cần thiết. Chưa thay trạng thái cơ sở thật trong lượt audit. |
| **P2** | `CancelBookingScopedAsync` cho Owner/Admin chỉ kiểm tra Pending/Confirmed; ít điều kiện hơn Customer cancel. | Cần quy định rõ hủy booking đã check-in/đã thanh toán/đã qua giờ và hoàn tiền. Đây là khoảng trống nghiệp vụ, không kết luận Owner có quyền hủy nhầm sân vì truy vấn Owner đã giới hạn OwnerId. |
| **P2** | `ReportService` tải nhiều danh sách rồi thống kê trong RAM; `HomeController.Index` lọc sau khi tải danh sách sân; chưa có pagination rõ cho các danh sách lớn. | Đủ cho demo nhỏ nhưng thiếu minh chứng hiệu năng khi dữ liệu tăng. Đẩy lọc/aggregate sang SQL, phân trang và đo query count/thời gian. |
| **P2** | Schema được quản lý đồng thời bởi migrations và nhiều hàm `Ensure*SchemaAsync`; seed runtime còn dựa OwnerId = 3 và tên địa phương. | Dễ drift schema/dữ liệu và khó chứng minh nâng cấp an toàn. Đưa thay đổi schema có version vào migrations; seed dev idempotent và lấy khóa thật thay vì giả định ID. |

## ❌ Những phần còn thiếu để bảo vệ chắc về chất lượng C# và backend

Không xem các mục này là yêu cầu chính thức của Thầy nếu chưa có rubric. Các nền tảng đã có như transaction booking, structured logging và advisory lock không bị đánh dấu thiếu toàn bộ.

1. **Chứng minh ACID cho nghiệp vụ nhiều bước ngoài booking.** Cần bảo đảm Facility + Courts cùng thành công/thất bại, test rollback trên PostgreSQL thật và kiểm thử restart không xóa dữ liệu. Booking transaction hiện có là điểm cộng; không thay thế tính nguyên tử của nghiệp vụ khác.
2. **Bộ test tích hợp HTTP/MVC.** Project test hiện chỉ tham chiếu Domain/Application/Infrastructure, chưa có WebApplicationFactory hoặc bài test controller/pipeline. Chưa có suite tự động cho login/role, anti-forgery, ModelState, returnUrl, view routing, AccountService và moderation. 23 kiểm tra HTTP lần này cung cấp bằng chứng audit, chưa phải pipeline regression có sẵn trong repository.
3. **Coverage và CI có thể lặp lại.** Đã đo coverage nhưng chưa có quality gate/CI workflow trong repository. Cần chạy test trong PostgreSQL test database riêng và lưu TRX/coverage. EF InMemory không chứng minh FK, PostgreSQL constraint, lock hay ExecuteUpdate translation. Hai integration tests có giá trị nhưng còn ít.
4. **Kiểm thử concurrency rộng hơn.** Chưa có test PostgreSQL cho voucher limit khi đặt nhiều sân song song, confirm đối đầu expiry, cancel đối đầu check-in, cập nhật voucher đối đầu booking, timeout/deadlock và truy cập chéo hai chủ sân qua HTTP. Hai request trùng slot chỉ xác nhận tình huống cơ bản.
5. **Thu hồi phiên và chống brute-force đăng nhập.** Chưa có security stamp/thu hồi cookie cũ và rate limiting/lockout theo số lần thử; việc khóa tài khoản ở service login chỉ ảnh hưởng lần login mới nếu cookie không được validate lại.
6. **Observability bền vững và audit nghiệp vụ.** BookingService/worker đã dùng ILogger có placeholders cho xác nhận/hủy/lỗi. Chưa có audit trail bền vững ghi actor, trạng thái trước/sau, reason, correlation ID và truy vết cập nhật cơ sở/voucher/moderation; chưa thấy request logging/health endpoint có kiểm tra DB. Không bắt buộc dùng Serilog để đạt mục tiêu này.
7. **Luồng thanh toán/hoàn tiền và đối soát.** Có entity Payments, PaymentStatus, cột báo cáo Paid/Refunded; chưa thấy controller/service thanh toán hoàn chỉnh, webhook/idempotency hoặc thao tác thu/hoàn tiền. Nếu ngoài phạm vi môn học thì ghi rõ giả lập/ngoài phạm vi, không demo như đã thu tiền thật.
8. **State machine và validation nhất quán.** Cần chốt Completed, review sau hoàn thành, giờ mở cửa riêng, hủy sau check-in/đã trả tiền; kiểm tra dữ liệu ở mọi đường gọi service để tránh mỗi controller áp dụng một cách.
9. **Minh chứng nâng cấp dữ liệu an toàn.** Có migrations và Docker volume nhưng chưa có kịch bản tự động backup/restore và smoke test sau nâng cấp, xác nhận giữ nguyên booking/review. Đây là phần cần nhất trước khi đồng bộ image hiện tại.

### Những lỗi test đã sửa trong lần đánh giá này

Lần chạy đầu: **103 pass, 6 fail, tổng 109**, hai test PostgreSQL khi chưa bật cấu hình trả về ngay nhưng vẫn bị tính pass. Sau sửa:

- `CourtServiceTests`, `OwnerCourtServiceTests`, `OwnerVoucherServiceTests`, `OwnerWalkInBookingTests`: ID fixture 5/10–13 trùng các sân seed hiện đã mở rộng đến ID 15. Chuyển fixture sang 105/110–113, giữ nguyên các assertion về quyền sở hữu/hiển thị.
- `OwnerFacilityServiceTests`: bổ sung CourtType và CourtTypeId theo yêu cầu tạo cơ sở mới; thêm PhoneNumber hợp lệ, assert success trước khi đọc kết quả; kiểm tra đủ ba sân đúng owner/type và Inactive trong lúc chờ duyệt.
- `PostgreSqlIntegrationTests`: dùng `PostgreSqlFactAttribute` để báo **skipped** khi chưa bật cấu hình, thay vì “pass” mà chưa chạy assertion. Bỏ connection string mặc định: bật integration test bắt buộc cung cấp `SPORTS_COURT_TEST_CONNECTION`, tránh vô tình dùng database demo. Đã chạy cả hai test thật trên database cô lập sau đó.

Không đổi thuật toán production để làm test vượt qua. [DemoScript.md](../DemoScript.md), [docs/DemoScript.md](DemoScript.md) và [CodeReview_QnA.md](CodeReview_QnA.md) đã cập nhật số liệu, hằng số thời gian và PBKDF2 đúng source.

## 💡 Các việc cần ưu tiên làm ngay trong 3–5 ngày tới

| Ngày | Việc cần hoàn thành | Tiêu chí nghiệm thu để trình bày với giảng viên |
| :--- | :--- | :--- |
| **Ngày 1** | Loại bỏ thao tác xóa dữ liệu khỏi startup; tách seed/dọn dev. Backup database và thử restore. Dựng bản kiểm thử với source mới trên DB sao chép, sau đó lên kế hoạch đồng bộ bản demo. | Restart bản thử hai lần không mất booking/review; migrate DB cũ thành công; ba route Admin đang 404 trả đúng trang; `Courts.District` được migrate. Chỉ rollout demo khi bảo toàn dữ liệu đã được chứng minh. |
| **Ngày 2** | Bọc nghiệp vụ tạo Facility + Courts trong một transaction hoặc một lần SaveChanges; chuẩn hóa validation Admin/facility; chốt và áp dụng giờ mở cửa. | Test PostgreSQL gây lỗi khi tạo sân không để lại Facility mồ côi; dữ liệu sai trả lỗi có nghĩa; backend và UI từ chối ngoài giờ của cơ sở. |
| **Ngày 3** | Thêm kiểm tra active/thu hồi phiên, chống thử login liên tục; củng cố worker khi DB lỗi. Bổ sung test concurrency voucher/confirm-expiry. | Cookie của tài khoản vừa bị khóa không thực hiện được thao tác; worker phục hồi sau lỗi DB; giới hạn voucher không bị vượt trên hai sân đồng thời. |
| **Ngày 4** | Chốt Completed/check-in/cancel/review, hoàn thiện public review hoặc loại use case chưa làm khỏi phạm vi demo; phân biệt doanh thu đặt chỗ với tiền đã thu. | Mọi transition có quy tắc và test; review sau hoàn thành hợp lệ; hủy đơn trả tiền có quy trình; báo cáo khớp dữ liệu kiểm tra. Không cần tích hợp cổng thanh toán thật nếu ngoài phạm vi đã thống nhất. |
| **Ngày 5** | Đưa HTTP regression và PostgreSQL tests vào CI; đo coverage lại, ưu tiên nhánh nghiệp vụ chưa phủ; phân trang/query aggregate nếu còn thời gian; tổng duyệt 6–8 phút và Q&A. | Build/test/HTTP smoke xanh từ checkout sạch; có TRX/coverage và danh sách test; demo đủ ba role, một ca thành công, một ca bị từ chối và giải thích đúng các hằng số thời gian. |

Nếu chỉ còn **3 ngày**, tập trung ngày 1–3 và giữ phạm vi demo khớp phần đã hoàn thiện. Không chạy theo số lượng tính năng mới trước khi xử lý nguy cơ mất dữ liệu và bản Docker chưa đồng bộ.

## Trạng thái bàn giao

- Hai DemoScript đã có mục **“5. Hướng dẫn Trả lời Câu hỏi Kiểm soát Code (Control Code Q&A)”**, bao gồm nội dung Q&A, bảng 8h / 10 phút / 15s / 06–22h / từ 17h / lock timeout 10s / UTC+7 và tài khoản Chủ sân.
- Seed và navbar CourtOwner đã tồn tại và được xác nhận đăng nhập thực tế, nên không tạo bản sao.
- Source build xanh; **107 test pass + 2 test PostgreSQL pass ở lượt chạy riêng**. Lượt mặc định hiển thị 2 skipped đúng cấu hình.
- Dữ liệu fixture riêng đã được dọn; database test riêng đã được xóa; Docker tiếp tục phục vụ trên cổng 5101.
- Các vấn đề production trong báo cáo là việc cần xử lý tiếp, không được coi là đã sửa trong lượt cập nhật tài liệu/đánh giá này. Đặc biệt, chưa rollout source mới lên Docker để tránh làm gián đoạn dịch vụ và kích hoạt startup cleanup hiện có.
