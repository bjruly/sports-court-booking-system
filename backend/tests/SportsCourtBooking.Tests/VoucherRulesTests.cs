using SportsCourtBooking.Application.Rules;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Tests;

public class VoucherRulesTests
{
    [Theory]
    [InlineData(80_000, 25, 20_000)]
    [InlineData(80_000, 100, 80_000)]
    public void CalculateDiscount_Percentage_UsesSubtotal(decimal subtotal, decimal value, decimal expected)
    {
        var discount = VoucherRules.CalculateDiscount(subtotal, new Voucher
        {
            DiscountType = VoucherDiscountType.Percentage,
            DiscountValue = value
        });

        Assert.Equal(expected, discount);
    }

    [Fact]
    public void CalculateDiscount_FixedAmountNeverExceedsSubtotal()
    {
        var discount = VoucherRules.CalculateDiscount(80_000m, new Voucher
        {
            DiscountType = VoucherDiscountType.FixedAmount,
            DiscountValue = 100_000m
        });

        Assert.Equal(80_000m, discount);
    }

    [Fact]
    public void ValidateApplication_RejectsExhaustedAndOutOfDateVouchers()
    {
        var today = new DateOnly(2026, 11, 17);
        var voucher = new Voucher
        {
            IsActive = true,
            StartDate = today,
            EndDate = today,
            UsageLimit = 1
        };

        Assert.NotNull(VoucherRules.ValidateApplication(voucher, today, usedCount: 1));
        Assert.NotNull(VoucherRules.ValidateApplication(voucher, today.AddDays(1), usedCount: 0));
    }

    [Fact]
    public void ValidateDefinition_RejectsPercentageAboveOneHundred()
    {
        var error = VoucherRules.ValidateDefinition(
            VoucherDiscountType.Percentage,
            100.01m,
            new DateOnly(2026, 11, 17),
            new DateOnly(2026, 11, 18),
            usageLimit: null);

        Assert.NotNull(error);
    }
}
