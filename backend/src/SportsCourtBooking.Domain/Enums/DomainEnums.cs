namespace SportsCourtBooking.Domain.Enums;

public enum UserRole
{
    Customer = 1,
    Staff = 2,
    Admin = 3,
    CourtOwner = 4
}

public enum CourtStatus
{
    Active = 1,       // Đang hoạt động
    Maintenance = 2,  // Đang bảo trì
    Inactive = 3      // Ngưng hoạt động
}

public enum BookingStatus
{
    Pending = 1,      // Chờ xác nhận / Chờ thanh toán
    Confirmed = 2,    // Đã xác nhận
    Cancelled = 3,    // Đã hủy
    Completed = 4,    // Đã hoàn thành ca chơi
    Expired = 5       // Hết thời hạn giữ chỗ
}

public enum PaymentStatus
{
    Unpaid = 1,
    Paid = 2,
    Refunded = 3,
    Failed = 4,
    DemoPaid = 5,
    DemoRefunded = 6
}

public enum PaymentMethodType
{
    VNPayDemo = 1,
    MoMoDemo = 2,
    BankTransferDemo = 3,
    CashAtVenue = 4
}

public enum RefundStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}
