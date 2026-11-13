using SportsCourtBooking.Application.Rules;

namespace SportsCourtBooking.Tests;

public class BookingRulesTests
{
    // Giả lập "bây giờ" theo giờ Việt Nam: 12/11/2026 lúc 10:00
    private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0);
    private static readonly DateOnly Today = new(2026, 11, 12);
    private static readonly DateOnly Tomorrow = new(2026, 11, 13);

    // ---------- Giá ----------

    [Theory]
    [InlineData(6, 80_000, 80_000)]
    [InlineData(16, 80_000, 80_000)]
    [InlineData(17, 80_000, 130_000)]
    [InlineData(21, 250_000, 300_000)]
    public void GetSlotPrice_AddsPeakSurchargeFrom17h(int hour, int basePrice, int expected)
    {
        Assert.Equal((decimal)expected, BookingRules.GetSlotPrice(basePrice, hour));
    }

    [Fact]
    public void CalculateTotalPrice_SumsEverySlotIncludingSurcharge()
    {
        // 16:00-19:00 sân 80.000đ: 80.000 + 130.000 + 130.000
        var total = BookingRules.CalculateTotalPrice(80_000m, new TimeOnly(16, 0), new TimeOnly(19, 0));

        Assert.Equal(340_000m, total);
    }

    // ---------- Chọn giờ ----------

    [Fact]
    public void TryNormalizeHours_AcceptsContiguousHours_AndSortsThem()
    {
        var ok = BookingRules.TryNormalizeHours(new[] { 10, 8, 9, 9 }, out var hours, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(new[] { 8, 9, 10 }, hours);
    }

    [Fact]
    public void TryNormalizeHours_RejectsNonContiguousHours()
    {
        // Trường hợp lỗi cũ: chọn 8h và 12h thì hệ thống từng đặt luôn 8h-13h
        var ok = BookingRules.TryNormalizeHours(new[] { 8, 12 }, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 5 })]            // trước giờ mở cửa
    [InlineData(new[] { 22 })]           // 22h không phải khung hợp lệ (đóng cửa)
    [InlineData(new[] { 23 })]           // lỗi cũ: new TimeOnly(24, 0) ném exception
    [InlineData(new[] { 8, 9, 10, 11, 12 })] // quá 4 khung
    public void TryNormalizeHours_RejectsInvalidSelections(int[] input)
    {
        var ok = BookingRules.TryNormalizeHours(input, out _, out var error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryNormalizeHours_RejectsNull()
    {
        Assert.False(BookingRules.TryNormalizeHours(null, out _, out _));
    }

    // ---------- Khoảng thời gian ----------

    [Fact]
    public void ValidateTimeRange_AcceptsNormalFutureBooking()
    {
        var error = BookingRules.ValidateTimeRange(Tomorrow, new TimeOnly(17, 0), new TimeOnly(19, 0), Now);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateTimeRange_AcceptsLastSlotEndingAtClosingTime()
    {
        var error = BookingRules.ValidateTimeRange(Tomorrow, new TimeOnly(21, 0), new TimeOnly(22, 0), Now);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(17, 0, 17, 0)]   // giờ kết thúc = giờ bắt đầu
    [InlineData(19, 0, 17, 0)]   // kết thúc trước bắt đầu
    [InlineData(17, 30, 19, 0)]  // không phải giờ tròn
    [InlineData(5, 0, 7, 0)]     // trước giờ mở cửa
    [InlineData(21, 0, 23, 0)]   // quá giờ đóng cửa
    [InlineData(6, 0, 11, 0)]    // 5 giờ liên tiếp, vượt tối đa 4 giờ
    public void ValidateTimeRange_RejectsInvalidRanges(int startHour, int startMinute, int endHour, int endMinute)
    {
        var error = BookingRules.ValidateTimeRange(
            Tomorrow, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute), Now);

        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void ValidateTimeRange_RejectsPastDate()
    {
        var error = BookingRules.ValidateTimeRange(new DateOnly(2026, 11, 11), new TimeOnly(17, 0), new TimeOnly(18, 0), Now);

        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData(9, true)]    // đã qua so với 10:00
    [InlineData(10, true)]   // khung 10:00 đã bắt đầu
    [InlineData(11, false)]  // khung 11:00 còn đặt được
    public void ValidateTimeRange_SameDay_OnlyAllowsSlotsAfterNow(int startHour, bool shouldBeRejected)
    {
        var error = BookingRules.ValidateTimeRange(Today, new TimeOnly(startHour, 0), new TimeOnly(startHour + 1, 0), Now);

        Assert.Equal(shouldBeRejected, error is not null);
    }

    // ---------- Khung giờ đã qua (dùng cho giao diện lịch) ----------

    [Theory]
    [InlineData(9, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    [InlineData(21, false)]
    public void IsSlotInPast_Today_DependsOnCurrentHour(int startHour, bool expected)
    {
        Assert.Equal(expected, BookingRules.IsSlotInPast(Today, startHour, Now));
    }

    [Fact]
    public void IsSlotInPast_PastDate_IsAlwaysTrue_AndFutureDate_IsAlwaysFalse()
    {
        Assert.True(BookingRules.IsSlotInPast(new DateOnly(2026, 11, 11), 21, Now));
        Assert.False(BookingRules.IsSlotInPast(Tomorrow, 6, Now));
    }
}
