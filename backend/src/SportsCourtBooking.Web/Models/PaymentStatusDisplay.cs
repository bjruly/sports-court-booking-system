using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

public static class PaymentStatusDisplay
{
    public static string Label(PaymentStatus? status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.DemoPaid => "Đã thanh toán mô phỏng",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        PaymentStatus.DemoRefunded => "Đã hoàn tiền mô phỏng",
        PaymentStatus.Failed => "Thanh toán thất bại",
        _ => "Chưa có thanh toán"
    };

    public static string MethodLabel(string? value)
    {
        if (!Enum.TryParse<PaymentMethodType>(value, out var method))
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        return method switch
        {
            PaymentMethodType.VNPayDemo => "VNPay demo",
            PaymentMethodType.MoMoDemo => "MoMo demo",
            PaymentMethodType.BankTransferDemo => "Chuyển khoản demo",
            PaymentMethodType.CashAtVenue => "Tiền mặt tại sân (demo)",
            _ => value ?? string.Empty
        };
    }
}
