using System.Net.Sockets;
using Npgsql;

namespace SportsCourtBooking.Desktop.Core;

public static class ErrorText
{
    /// <summary>Đổi lỗi kỹ thuật thành câu người dùng hiểu được.</summary>
    public static string For(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case PostgresException { SqlState: PostgresErrorCodes.InvalidPassword or PostgresErrorCodes.InvalidAuthorizationSpecification }:
                    return "Database từ chối tài khoản kết nối. Hãy kiểm tra mật khẩu PostgreSQL trong cấu hình.";
                case PostgresException { SqlState: PostgresErrorCodes.InvalidCatalogName or PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.UndefinedColumn }:
                    return "Database chưa được khởi tạo hoặc chưa cập nhật phiên bản mới. Hãy chạy web một lần để cập nhật (docker compose up --build).";
                case SocketException or TimeoutException:
                    return "Không kết nối được database. Hãy kiểm tra PostgreSQL đang chạy (docker compose up -d db).";
            }
        }

        return "Đã xảy ra lỗi: " + exception.GetBaseException().Message;
    }
}
