using System.Security.Claims;

namespace SportsCourtBooking.Web.Extensions;

public static class UserClaimsExtensions
{
    /// <summary>Lấy Id người dùng đang đăng nhập từ cookie xác thực (không bao giờ tin dữ liệu form gửi lên).</summary>
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
