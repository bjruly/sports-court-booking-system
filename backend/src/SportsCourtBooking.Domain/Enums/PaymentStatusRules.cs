namespace SportsCourtBooking.Domain.Enums;

public static class PaymentStatusRules
{
    public static bool IsPaid(PaymentStatus? status)
        => status is PaymentStatus.Paid or PaymentStatus.DemoPaid;

    public static bool IsRefunded(PaymentStatus? status)
        => status is PaymentStatus.Refunded or PaymentStatus.DemoRefunded;
}
