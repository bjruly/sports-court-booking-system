namespace SportsCourtBooking.Infrastructure.Data;

/// <summary>
/// Mật khẩu đã băm (PBKDF2) của các tài khoản mẫu trong dữ liệu seed.
///   admin@sportscourt.vn  /  Admin@123
///   khachhang@gmail.com   /  Customer@123
///   chusan@sportscourt.vn /  Owner@123
/// Đây chỉ là tài khoản demo cho môi trường phát triển. Hãy đổi mật khẩu hoặc xóa khi triển khai thật.
/// </summary>
public static class SeedCredentials
{
    public const string OwnerEmail = "chusan@sportscourt.vn";
    public const string OwnerPassword = "Owner@123";

    public const string AdminPasswordHash =
        "PBKDF2$600000$neM7qB+leDO3+vsCkeLRxw==$zgOyy5kLxJe8Y0A66njeoVQkOY2p1w8COMVP3CSIBAM=";

    public const string CustomerPasswordHash =
        "PBKDF2$600000$5zSDSSQexjU4TVYBtlvD1w==$CSFQdFZ+j7co9czmOtljXi275U1w19S10KR4SemDpjw=";

    public const string OwnerPasswordHash =
        "PBKDF2$600000$WIPJmJtUj/GYI3gApkYU3w==$FhZSko39SjFTD2XfLcVCyZkoOX2p7dS7YWm0gbTo7R4=";
}
