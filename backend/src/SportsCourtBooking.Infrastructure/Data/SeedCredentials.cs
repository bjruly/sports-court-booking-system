namespace SportsCourtBooking.Infrastructure.Data;

/// <summary>
/// Mật khẩu đã băm (PBKDF2) của 2 tài khoản mẫu trong dữ liệu seed.
///   admin@sportscourt.vn  /  Admin@123
///   khachhang@gmail.com   /  Customer@123
/// Đây chỉ là tài khoản demo cho môi trường phát triển. Hãy đổi mật khẩu hoặc xóa khi triển khai thật.
/// Giá trị phải khớp với migration 20261112090000_HashSeedPasswords.
/// </summary>
public static class SeedCredentials
{
    public const string AdminPasswordHash =
        "PBKDF2$600000$neM7qB+leDO3+vsCkeLRxw==$zgOyy5kLxJe8Y0A66njeoVQkOY2p1w8COMVP3CSIBAM=";

    public const string CustomerPasswordHash =
        "PBKDF2$600000$5zSDSSQexjU4TVYBtlvD1w==$CSFQdFZ+j7co9czmOtljXi275U1w19S10KR4SemDpjw=";
}
