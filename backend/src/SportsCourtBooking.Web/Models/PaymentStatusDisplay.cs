using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

public static class PaymentStatusDisplay
{
    public static string Label(PaymentStatus? status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid or PaymentStatus.DemoPaid => "Đã thanh toán",
        PaymentStatus.Refunded or PaymentStatus.DemoRefunded => "Đã hoàn tiền",
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
            PaymentMethodType.VNPayDemo => "VNPay",
            PaymentMethodType.MoMoDemo => "MoMo",
            PaymentMethodType.BankTransferDemo => "Chuyển khoản",
            PaymentMethodType.CashAtVenue => "Tiền mặt tại sân",
            _ => value ?? string.Empty
        };
    }
}
