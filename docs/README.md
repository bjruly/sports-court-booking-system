# Tài liệu dự án

Thư mục này lưu báo cáo, sơ đồ phân tích/thiết kế và tài liệu tham khảo.

| File | Nội dung |
|---|---|
| `BaoCao_Nhom_DatSanTheThao.docx` | Báo cáo phân tích hệ thống, khảo sát công nghệ và yêu cầu nghiệp vụ. |
| `01_C4_Context.drawio` | Bối cảnh hệ thống và các tác nhân/dịch vụ bên ngoài. |
| `02_C4_Container.drawio` | Các container chính và cách chúng giao tiếp. |
| `03_Workflow.drawio` | Quy trình tìm sân, giữ chỗ, thanh toán và xử lý đơn. |
| `04_UseCase.drawio` | Use case theo vai trò khách hàng, chủ sân và quản trị viên. |
| `05_ERD.drawio` | Mô hình dữ liệu và quan hệ giữa các thực thể. |
| `06_Review_Checklist.md` | Checklist điều phối/review phạm vi MVP, vòng đời booking, ERD và wireframe. |
| `07_UI_Wireframe.drawio` | Prototype trực quan các màn hình web MVP. |
| `07_UI_Wireframe.md` | Luồng UI, trạng thái màn hình và tiêu chí review/acceptance. |
| `TLTK/` | Tài liệu mẫu và nguồn tham khảo được liệt kê hoặc dùng để định hướng báo cáo. |

**Lựa chọn công nghệ:** báo cáo và solution hiện thống nhất dùng ASP.NET Core MVC + Razor bằng C# theo kiến trúc modular monolith; PostgreSQL là cơ sở dữ liệu đề xuất. Cấu hình lưu trữ và các nghiệp vụ sẽ được bổ sung trong giai đoạn triển khai.

## Tài liệu tham khảo chính

Danh mục đầy đủ nằm ở cuối `BaoCao_Nhom_DatSanTheThao.docx`. Các nguồn kỹ thuật chính có thể tra cứu trực tuyến:

- Simon Brown, *The C4 model for visualising software architecture* (2022): [c4model.com](https://c4model.com/)
- PostgreSQL 16 Documentation: [Range Types](https://www.postgresql.org/docs/16/rangetypes.html), [Constraints](https://www.postgresql.org/docs/16/ddl-constraints.html), [btree_gist](https://www.postgresql.org/docs/16/btree-gist.html).
- Microsoft Learn: [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-8.0), [MVC role authorization](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/roles?view=aspnetcore-8.0), [Entity Framework Core](https://learn.microsoft.com/ef/core/) và [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).
- OWASP Foundation, [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- VNPAY, [hướng dẫn tích hợp cổng thanh toán PAY](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html).

Các tài liệu được trích dẫn bằng thông tin thư mục và liên kết; không cần lưu bản sao các tài liệu bên thứ ba trong repository.
