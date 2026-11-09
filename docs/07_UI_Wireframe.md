# Wireframe giao diện MVP

## Mục tiêu và phạm vi

Tài liệu này đặc tả giao diện web responsive cho MVP ASP.NET Core MVC/Razor và là tài liệu tham chiếu để nhóm review. Một phần giao diện đã được triển khai thành prototype gồm tìm sân, lịch/chọn giờ, xem lại lựa chọn và màn hình đăng nhập/đăng ký. Prototype vẫn dùng dữ liệu mẫu; đăng nhập, lưu booking và kết nối PostgreSQL chưa được triển khai.

Luồng chính:

1. Người dùng chưa đăng nhập có thể xem danh sách sân và lịch.
2. Khi bắt đầu đặt sân, hệ thống yêu cầu đăng nhập; người chưa có tài khoản có thể đăng ký rồi đăng nhập.
3. Sau khi xác thực, hệ thống kiểm tra lại các khung giờ đã chọn; nếu có giờ vừa được người khác đặt thì báo và yêu cầu chọn lại.
4. Khi hợp lệ, khách xác nhận thông tin và tạo booking `PENDING`.
5. MVP không tích hợp hoặc mô phỏng thanh toán. `PENDING` không có nghĩa là đã được chủ sân xác nhận hoặc đã thanh toán.

## Danh sách màn hình

| Mã | Màn hình | Mục đích |
|---|---|---|
| UI-01 | Đăng ký | Tạo tài khoản khách hàng. |
| UI-02 | Đăng nhập | Xác thực người dùng và quay lại luồng đặt sân. |
| UI-03 | Tìm sân | Tìm/lọc và mở chi tiết cơ sở hoặc sân. |
| UI-04 | Lịch sân và chọn giờ | Xem giá/trạng thái giờ, chọn tối đa 4 khung liên tiếp trên cùng sân/ngày. |
| UI-05 | Xác nhận đặt sân | Rà soát sân, giờ, giá trước khi tạo booking. |
| UI-06 | Đã tạo booking | Hiển thị mã booking và trạng thái `PENDING`, không hiển thị thành công thanh toán. |
| UI-07 | Lịch sân trên mobile | Minh họa bố cục responsive và thao tác chọn giờ trên màn hình nhỏ. |

Các màn hình có thể được nhóm thành cùng một trang Razor nếu phù hợp với cấu trúc route; mã UI chỉ dùng để review và truy vết yêu cầu.

## Mô tả màn hình và trạng thái

### UI-01 — Đăng ký

- Trường: họ tên, email, mật khẩu, nhập lại mật khẩu; số điện thoại tùy quyết định nghiệp vụ.
- Kiểm tra đầu vào tại server; không chỉ dựa vào validation phía trình duyệt.
- Nút chính: **Tạo tài khoản**. Liên kết phụ sang đăng nhập.
- Trạng thái: mặc định, đang gửi, lỗi dữ liệu/email đã tồn tại, tạo tài khoản thành công.
- Sau khi đăng ký thành công, đăng nhập người dùng hoặc đưa họ tới đăng nhập theo quyết định triển khai; nếu quay lại đặt sân thì phải kiểm tra lại giờ đã chọn.

### UI-02 — Đăng nhập

- Trường email và mật khẩu; có hiện/ẩn mật khẩu và liên kết sang đăng ký.
- Nút chính: **Đăng nhập**.
- Trạng thái: mặc định, đang xác thực, sai thông tin, tài khoản bị khóa/chưa được phép đăng nhập, thành công.
- Khi khách vào bước đặt sân nhưng chưa đăng nhập, giữ ngữ cảnh sân/giờ để quay lại; sau đăng nhập phải kiểm tra lại khả dụng, không đảm bảo giờ vẫn còn.

### UI-03 — Tìm sân

- Thanh điều hướng: thương hiệu, tìm sân, lịch đặt của tôi, trạng thái đăng nhập/đăng nhập.
- Bộ lọc MVP: môn thể thao, khu vực, ngày; các bộ lọc chưa có dữ liệu có thể để sau.
- Mỗi thẻ sân: tên cơ sở/sân, khu vực, môn, giá từ, nút **Xem lịch**.
- Tên cơ sở, giá và ngày trong Draw.io chỉ là dữ liệu minh họa, không phải dữ liệu kinh doanh đã chốt.
- Trạng thái: tải dữ liệu, có kết quả, không có kết quả, lỗi tải kèm thao tác thử lại.
- Không hiển thị tính năng voucher, đánh giá hoặc quản trị như phần đã triển khai trong MVP.

### UI-04 — Lịch sân và chọn giờ

- Phần đầu: tên cơ sở/sân, ngày đã chọn, điều hướng ngày trước/sau.
- Lưới giờ 1 giờ với chú giải rõ: **Còn trống**, **Đang giữ/đã đặt**, **Đã chọn**, **Không khả dụng**.
- Giá hiển thị theo từng khung; tổng tiền cập nhật khi chọn giờ.
- Chỉ cho chọn 1–4 giờ liên tiếp trên cùng sân và ngày. Không hợp thức hóa lựa chọn bằng client; server kiểm tra lại.
- Trạng thái: đang tải; lưới trống; có giờ; không còn giờ; giờ vừa bị đặt; lỗi tải.
- Nếu chọn giờ rời rạc hoặc quá 4 giờ, hiển thị lý do và giữ nguyên các lựa chọn hợp lệ hoặc yêu cầu chọn lại một cách nhất quán.

### UI-05 — Xác nhận đặt sân

- Tóm tắt cơ sở/sân, ngày, các khung giờ, giá từng khung và tổng tiền.
- Mã voucher, thanh toán và hủy không xuất hiện trong luồng MVP.
- Nút chính: **Tạo booking**; nút phụ quay lại lịch.
- Trạng thái: mặc định; đang gửi (khóa nút để tránh gửi lặp); tạo thành công; giờ không còn trống; dữ liệu không hợp lệ; lỗi hệ thống.
- Nếu người dùng chưa đăng nhập, chuyển sang UI-02/UI-01; quay lại phải kiểm tra lại lịch.

### UI-06 — Kết quả booking

- Hiển thị mã booking, trạng thái `PENDING`, sân/giờ đã chọn và tổng tiền tham khảo.
- Ghi rõ: **Chưa thanh toán — booking chưa được xác nhận**.
- Không hiển thị nút “Thanh toán thành công” hay trạng thái `CONFIRMED`.
- Thời hạn giữ/đếm ngược chỉ hiển thị sau khi nhóm chốt chính sách giữ chỗ và backend thực thi chính sách đó.
- Trạng thái lỗi tạo booking dẫn người dùng quay lại lịch; các giờ cần tải mới từ server.

### UI-07 — Lịch sân trên mobile

- Thanh điều hướng gọn, tên sân và ngày dễ nhận biết.
- Các nút giờ xếp thành lưới vừa màn hình; trạng thái đi kèm nhãn/chú giải.
- Tóm tắt số giờ và tổng tiền nằm gần nút tiếp tục, không che nội dung lịch.
- Prototype là bố cục minh họa; khi code cần kiểm tra trên kích thước màn hình nhỏ và khả năng cuộn/chạm.

## Quy tắc tương tác và bảo vệ dữ liệu

- Các trang đọc danh mục/lịch có thể xem khi chưa đăng nhập; tạo booking yêu cầu tài khoản đã đăng nhập.
- Không coi lựa chọn giờ trên trình duyệt là giữ chỗ. Chỉ khi server tạo booking thành công mới có booking `PENDING`.
- Khi gửi yêu cầu, server kiểm tra tài khoản, sân, ngày giờ, quy tắc 1–4 khung liên tiếp, giá và khả dụng trong transaction.
- Nếu hai khách chọn cùng giờ, chỉ một yêu cầu được tạo booking; yêu cầu còn lại nhận thông báo giờ không còn trống và tải lại lịch.
- Sau đăng ký/đăng nhập, mọi giờ đã chọn phải được kiểm tra lại vì có thể đã bị người khác đặt trong lúc xác thực.
- Dùng thông báo lỗi dễ hiểu; không lộ chi tiết lỗi database hoặc exception cho người dùng.

## Responsive và định hướng giao diện

- Desktop: thanh điều hướng ngang; bộ lọc và nội dung dạng cột; lịch hiển thị lưới rõ ràng.
- Mobile: điều hướng gọn; bộ lọc xếp dọc; lưới giờ có thể cuộn ngang; thanh tóm tắt tổng tiền/nút tiếp tục dễ thao tác.
- Ngôn ngữ giao diện: tiếng Việt. Prototype dùng nền sáng, chữ tương phản cao, màu xanh đậm cho điều hướng và xanh lá cho hành động chính; màu trạng thái cần đi kèm nhãn, không chỉ dựa vào màu.
- Các lựa chọn typography, màu và khoảng cách trong Draw.io là gợi ý để thống nhất prototype, có thể điều chỉnh sau review.

## Tiêu chí review/acceptance

1. Có đường đi từ danh sách sân tới lịch, đăng nhập/đăng ký và quay lại luồng chọn giờ.
2. Người chưa đăng nhập không thể tạo booking; sau đăng nhập hệ thống kiểm tra lại lịch.
3. Người dùng chỉ chọn được tối đa bốn khung liên tiếp, mỗi khung một giờ, trên cùng sân/ngày.
4. Tổng giá khớp tổng các khung đã chọn và không có voucher trong MVP.
5. Màn hình kết quả phân biệt rõ `PENDING` với đã xác nhận/đã thanh toán.
6. Có thiết kế trạng thái tải, rỗng, lỗi, gửi lặp và xung đột giờ.
7. Không có nội dung UI hứa giữ giờ trong thời lượng cụ thể cho tới khi chính sách giữ chỗ được chốt và triển khai.

## Câu hỏi còn cần quyết định trước khi code

- Số điện thoại có bắt buộc khi đăng ký không?
- Đăng ký thành công tự đăng nhập hay yêu cầu đăng nhập lại?
- Màn hình chi tiết cơ sở và lịch sân dùng một trang hay hai route?
- Thời hạn giữ booking `PENDING` là bao lâu và UX hiển thị hết hạn thế nào?

## Wireframe trực quan

Mở [07_UI_Wireframe.drawio](./07_UI_Wireframe.drawio) bằng diagrams.net để xem từng màn hình prototype, gồm bố cục lịch trên desktop và mobile.
