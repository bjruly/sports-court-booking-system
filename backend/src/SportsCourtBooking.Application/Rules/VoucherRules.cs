using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.Rules;

public static class VoucherRules
{
    public static string? ValidateDefinition(
        VoucherDiscountType discountType,
        decimal discountValue,
        DateOnly? startDate,
        DateOnly? endDate,
        int? usageLimit)
    {
        if (!Enum.IsDefined(discountType))
        {
            return "Loại giảm giá không hợp lệ.";
        }

        if (discountValue <= 0
            || (discountType == VoucherDiscountType.Percentage && discountValue > 100))
        {
            return "Giá trị giảm không hợp lệ.";
        }

        if (!startDate.HasValue || !endDate.HasValue || endDate.Value < startDate.Value)
        {
            return "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.";
        }

        if (usageLimit is <= 0)
        {
            return "Giới hạn lượt dùng phải lớn hơn 0.";
        }

        return null;
    }

    public static string? ValidateApplication(Voucher voucher, DateOnly applicationDate, int usedCount)
    {
        if (!voucher.IsActive)
        {
            return "Voucher hiện không hoạt động.";
        }

        if (applicationDate < voucher.StartDate || applicationDate > voucher.EndDate)
        {
            return "Voucher không có hiệu lực vào ngày đặt sân đã chọn.";
        }

        if (voucher.UsageLimit.HasValue && usedCount >= voucher.UsageLimit.Value)
        {
            return "Voucher đã hết lượt sử dụng.";
        }

        return null;
    }

    public static decimal CalculateDiscount(decimal subtotal, Voucher voucher)
    {
        if (subtotal <= 0)
        {
            return 0m;
        }

        var discount = voucher.DiscountType switch
        {
            VoucherDiscountType.Percentage => subtotal * voucher.DiscountValue / 100m,
            VoucherDiscountType.FixedAmount => voucher.DiscountValue,
            _ => 0m
        };

        return decimal.Round(Math.Min(subtotal, discount), 2, MidpointRounding.AwayFromZero);
    }
}
