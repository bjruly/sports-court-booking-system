using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Infrastructure.Data;

public static class DemoOwnerExtensions
{
    /// <summary>
    /// Id thực tế của tài khoản Chủ Sân mẫu. Không được giả định Id = 3: ở database cũ, Id này có thể
    /// thuộc về một khách hàng đã tự đăng ký trước khi tài khoản Chủ Sân mẫu được tạo.
    /// </summary>
    public static Task<int> GetDemoOwnerIdAsync(this AppDbContext db, CancellationToken cancellationToken = default) =>
        db.Users
            .Where(user => user.Email == SeedCredentials.OwnerEmail && user.Role == UserRole.CourtOwner)
            .Select(user => user.Id)
            .SingleAsync(cancellationToken);
}
