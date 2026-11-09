# Checklist điều phối và review

> Đây là tài liệu hỗ trợ buổi làm việc của nhóm, không phải biên bản quyết định đã được thông qua. Chỉ chuyển mục sang “Đã chốt” sau khi cả nhóm thống nhất.

## Mục tiêu

1. Rà soát tính khả thi và nhất quán của phạm vi MVP đã chốt; không mở rộng sang toàn bộ 26 user story.
2. Review vòng đời booking, schema và wireframe hiện có.
3. Ghi lại quyết định và việc còn mở để code, database, giao diện và kiểm thử cùng dựa trên một phiên bản.

## Chuẩn bị trước buổi họp

- Nhóm cùng rà soát các hiện vật: luồng/state booking, ERD/schema, wireframe và kịch bản nghiệm thu. Bản wireframe hiện tại là prototype ban đầu, cần được đối chiếu và chỉnh theo quyết định nhóm.
- Đi lần lượt từ phạm vi MVP → luồng người dùng → quy tắc booking → schema → wireframe → tiêu chí nghiệm thu; mỗi bước dựa trên kết quả đã thống nhất ở bước trước.
- Ghi rõ câu hỏi chưa thống nhất và điều kiện cần để ra quyết định.

## Phạm vi MVP

Phạm vi MVP đã được nhóm trưởng chốt:

1. Khách đăng nhập; người chưa có tài khoản đăng ký trước.
2. Xem danh sách cơ sở/sân.
3. Xem giờ còn trống và giá cho một sân, một ngày.
4. Chọn tối đa bốn khung giờ 1 giờ liên tiếp trên cùng sân/ngày và tạo booking `PENDING`.
5. Từ chối khung giờ không hợp lệ hoặc đã được giữ/đặt.

Khách vãng lai/đặt hộ, thanh toán thật, voucher, hủy/hoàn tiền, thông báo ngoài hệ thống và màn hình quản trị nằm ngoài MVP. Booking `PENDING` không đồng nghĩa đã xác nhận hoặc thanh toán. Các chi tiết thời hạn giữ, hết hạn/nhả giờ và chống đặt trùng vẫn cần được review trước khi triển khai.

### Kết quả rà soát tài liệu

Đây là nhận xét từ việc đối chiếu tài liệu hiện có, không thay cho quyết định của cả nhóm.

| Mức ưu tiên | Phát hiện | Cần làm rõ trong buổi review |
|---|---|---|
| Cần cập nhật tài liệu | UC05 chuyển ngay sang UC07 thanh toán; workflow cũng có thanh toán online, ngoài phạm vi MVP đã chốt. | Giữ luồng thanh toán trong thiết kế dài hạn nhưng phân biệt rõ với MVP; MVP kết thúc ở `PENDING`, không mô phỏng xác nhận thanh toán. |
| Cần cập nhật schema theo phạm vi | ERD yêu cầu `booking.customer_id`, trong khi báo cáo cho phép chủ sân tạo đơn cho khách đến quầy và có nhắc khách không có tài khoản. | MVP chỉ có khách đã đăng nhập; giữ guest/walk-in ngoài phạm vi hiện tại. Cập nhật ERD/use case để phân biệt MVP với thiết kế dài hạn. |
| Chặn review schema | ERD vẫn ghi `app_user.password_hash VARCHAR(100)`; hướng thiết kế trong báo cáo dùng kiểu không gắn cứng độ dài hash. | Sửa ERD theo quyết định Identity đã chọn; không tự lưu/định dạng mật khẩu bằng logic riêng của ứng dụng. |
| Chặn kiểm soát lịch | BR01 nói chỉ một booking PENDING/CONFIRMED được giữ giờ; BR02 nói PENDING hết hạn sau 10 phút. ERD có cả `booking.status` lẫn `booking_item.active`, nên trạng thái giữ giờ có thể bị biểu diễn hai lần. | Làm rõ nguồn trạng thái có thẩm quyền, cách cập nhật nguyên tử booking và các item, cùng cách xử lý booking đã quá hạn nhưng tác vụ nền chưa chạy. |
| Ngoài phạm vi MVP? | UC05 hỗ trợ 1–4 giờ liên tiếp, voucher và màn hình ghi thông tin; workflow có hủy/hoàn tiền; ERD đã chứa payment/refund/voucher. | Giữ thanh toán, voucher và hủy/hoàn tiền ở thiết kế dài hạn/backlog; sơ đồ phải phân biệt rõ với phạm vi demo. |
| Cần review prototype | Đã có wireframe ban đầu cho đăng ký, đăng nhập, tìm sân, lịch desktop/mobile, xác nhận và kết quả booking. | Đối chiếu với luồng MVP, xác nhận nội dung/trạng thái và phản hồi các màn hình cần chỉnh. |

#### Các nội dung cần review kỹ thuật

- Phạm vi MVP nêu trên đã chốt; buổi review tập trung vào tính nhất quán và khả thi của nghiệp vụ/schema.
- Review thời hạn giữ `PENDING` (10 phút đang là đề xuất trong báo cáo), cách hết hạn/nhả giờ và các kết quả kỳ vọng. Không trình bày `PENDING` như một đơn đã thanh toán.
- Review cách dùng PostgreSQL/transaction để ngăn đặt trùng và giữ nhất quán booking với booking_item. Booking quá hạn không được tiếp tục chiếm giờ chỉ vì tác vụ nền chưa chạy.
- Đưa wireframe, ERD cập nhật và sơ đồ trạng thái/luồng hết hạn vào cùng lượt review.

## Checklist review theo luồng

### 1. Người dùng và xác thực

- [ ] Luồng đặt sân chỉ dành cho khách đã đăng nhập; khách vãng lai/đặt hộ không xuất hiện trong MVP.
- [ ] Mọi booking MVP gắn với một người dùng hợp lệ.
- [ ] Mô hình người dùng/role trong ERD có cách ánh xạ sang ASP.NET Core Identity; không lưu mật khẩu thuần.

### 2. Sân, giá và khung giờ

- [ ] Quy ước múi giờ rõ ràng; lưu thời điểm theo UTC và hiển thị theo giờ địa phương.
- [ ] Xác nhận đơn vị khung giờ (báo cáo hiện mô tả một giờ) và biên khoảng thời gian `[bắt đầu, kết thúc)` để hai khung liền kề không bị xem là trùng.
- [ ] Xác định sân/ngày/giờ đã qua, ngoài giờ mở cửa, sân không hoạt động và khoảng bảo trì được biểu diễn thế nào.
- [ ] Giao diện hiển thị đủ giá và trạng thái giờ trống/đang giữ/đã đặt để người dùng không chọn nhầm.

### 3. Vòng đời booking

- [ ] Liệt kê các trạng thái, sự kiện làm đổi trạng thái và vai trò được phép thực hiện chuyển đổi.
- [ ] Xác nhận thời hạn giữ chỗ 10 phút được đề xuất trong báo cáo; xác định mốc bắt đầu tính hạn.
- [ ] Quy định rõ hết hạn khi tác vụ nền chạy trễ: luồng tạo booking mới không được coi một lượt giữ đã quá hạn là còn hiệu lực.
- [ ] Chuyển booking sang hết hạn và nhả các khung giờ phải nguyên tử; hai yêu cầu đồng thời không được cùng giữ một khung giờ.
- [ ] Demo kết thúc ở `PENDING`; không có thanh toán giả lập. Xác định cách ghi nhận booking hết hạn và khung giờ được mở lại.
- [ ] Nếu thanh toán thành công sau khi booking hết hạn, áp dụng nguyên tắc báo cáo đã nêu: không chiếm lại giờ đã được đặt; xử lý hoàn tiền/đối soát theo luồng riêng khi tích hợp thanh toán.
- [ ] Sơ đồ `03_Workflow.drawio` mô tả thanh toán online và hủy/hoàn tiền; thống nhất đây là thiết kế dài hạn hay cần cập nhật để phản ánh phạm vi MVP.

### 4. Schema và toàn vẹn dữ liệu

- [ ] Chỉ ra ràng buộc cơ sở dữ liệu ngăn các khoảng giờ chồng lấn trên cùng sân; kiểm tra có bao phủ cả booking đang giữ và booking đã xác nhận.
- [ ] Các bản ghi booking_item phải được tạo, nhả hoặc hết hiệu lực cùng transaction với booking.
- [ ] Kiểm tra unique cho mã booking, email/tài khoản và mã voucher theo phạm vi nghiệp vụ đã chọn.
- [ ] Xác định giá tại thời điểm đặt là snapshot; thay đổi bảng giá sau đó không làm đổi số tiền của booking cũ.
- [ ] ERD có `customer_id` bắt buộc nhưng báo cáo cũng đề cập khách đến quầy; quyết định mô hình khách walk-in trước khi triển khai tính năng đó.
- [ ] ERD có `voucher.used_count`; nếu nhiều booking PENDING cùng áp dụng voucher, cần chốt cách giữ quota nguyên tử. Nếu chưa xử lý được trong MVP, loại voucher khỏi phạm vi demo.
- [ ] Báo cáo dùng chính sách hủy theo cơ sở; quyết định booking cũ có giữ snapshot chính sách tại lúc đặt hay luôn dùng chính sách hiện hành.

### 5. Wireframe và nghiệm thu

- [ ] Wireframe thể hiện trạng thái tải, không có kết quả, giờ vừa bị người khác đặt và lỗi khi tạo booking.
- [ ] Có thông báo thành công kèm mã booking/trạng thái và bước tiếp theo người dùng cần làm.
- [ ] Chuyển từng tiêu chí thành kịch bản có dữ liệu vào, thao tác và kết quả mong đợi; không dùng tiêu chí “hoạt động tốt” chung chung.
- [ ] Báo cáo hiện có 26 user story nhưng chỉ 6 story có tiêu chí nghiệm thu chi tiết; thống nhất story nào thuộc lát cắt đầu tiên và bổ sung tiêu chí còn thiếu.
- [ ] Kiểm thử trường hợp hai yêu cầu đồng thời trên cùng giờ; một yêu cầu thành công, yêu cầu còn lại nhận thông báo giờ không còn trống.

## Trình tự review

| Bước | Nội dung |
|---|---|
| 1 | Đối chiếu luồng và wireframe với phạm vi MVP đã chốt; ghi điểm sai khác cần cập nhật. |
| 2 | Đi qua luồng người dùng và wireframe hiện có; ghi màn hình/trạng thái còn thiếu. |
| 3 | Review trạng thái booking, hết hạn và trường hợp cạnh tranh. |
| 4 | Review ERD, quan hệ và ràng buộc chống đặt trùng. |
| 5 | Đối chiếu luồng với kịch bản nghiệm thu. |
| 6 | Đọc lại quyết định, việc tiếp theo, thứ tự phụ thuộc và trở ngại. |

## Sổ quyết định của buổi họp

Ghi ngay trong buổi họp. Không suy diễn quyết định từ nội dung đề xuất trong báo cáo.

| Chủ đề | Trạng thái | Quyết định/việc cần làm |
|---|---|---|
| Phạm vi MVP và thanh toán | Đã chốt | MVP dừng ở booking `PENDING`; không tích hợp hoặc mô phỏng thanh toán. |
| Khách vãng lai/đặt hộ | Đã chốt | Ngoài MVP; chỉ khách đăng nhập được đặt sân trong lát cắt này. |
| Quy tắc chọn khung giờ | Đã chốt | Một khung 1 giờ; tối đa 4 khung liên tiếp trên cùng sân và ngày. |
| Trạng thái booking và thời hạn giữ | Chờ review đề xuất | `PENDING` chưa xác nhận/thanh toán; thời hạn 10 phút là đề xuất trong tài liệu, cần rà soát. |
| Chống đặt trùng và transaction | Chờ review schema | Làm rõ cơ chế PostgreSQL và nguồn trạng thái giữ giờ duy nhất. |
| Voucher quota trong lúc PENDING | Ngoài MVP | Không triển khai voucher trong lát cắt đầu tiên. |
| Chính sách hủy cho booking cũ | Ngoài MVP | Không triển khai hủy/hoàn tiền trong lát cắt đầu tiên. |
| Wireframe màn hình MVP | Có prototype, chờ review | Đối chiếu với luồng MVP và thống nhất nội dung cần chỉnh. |
| ERD và kiểu password_hash | Chờ review/cập nhật | ERD hiện ghi `VARCHAR(100)`; cần khớp lựa chọn ASP.NET Core Identity. |
| Story và tiêu chí nghiệm thu thuộc MVP | Chờ nhóm xác nhận | Ánh xạ story vào luồng MVP và ghi expected result cụ thể. |

## Tài liệu cần đối chiếu

- [Báo cáo đồ án](./BaoCao_Nhom_DatSanTheThao.docx)
- [Sơ đồ quy trình](./03_Workflow.drawio)
- [Sơ đồ ERD](./05_ERD.drawio)
- [Trình tự công việc tiếp theo](../README.md)
