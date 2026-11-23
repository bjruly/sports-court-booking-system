using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class AdminReviewsPage : ListPage<ReviewItemDto>
{
    private static readonly string[] HideReasons =
    {
        "Ngôn từ thô tục, xúc phạm hoặc bôi nhọ người khác",
        "Spam, quảng cáo hoặc tiếp thị dịch vụ trái phép",
        "Thông tin bịa đặt, sai sự thật gây ảnh hưởng uy tín sân",
        "Nội dung không liên quan đến trải nghiệm thực tế tại sân"
    };

    private readonly StatCard _total = new("Tổng đánh giá");
    private readonly StatCard _visible = new("Đang hiển thị");
    private readonly StatCard _reported = new("Báo cáo vi phạm");
    private readonly StatCard _hidden = new("Đã bị ẩn");
    private readonly TextBox _search;
    private readonly ComboBox _filter;

    public AdminReviewsPage(AppServices app, IShell shell)
        : base(app, shell, "Xử lý đánh giá vi phạm", "Ẩn, khôi phục hoặc xóa đánh giá của khách.", item => item.Id)
    {
        Grid.EmptyText = "Không có đánh giá nào phù hợp.";
        Grid.Add("Khách hàng", item => $"{item.UserName}{Environment.NewLine}{item.UserEmail}", 160)
            .Add("Sân", item => $"{item.CourtName}{Environment.NewLine}{item.CourtTypeName}", 150)
            .Add("Sao", item => $"{item.Rating}/5 ★", 60, sort: item => item.Rating)
            .Add("Nội dung", item => item.Comment, 260)
            .Add("Ngày đăng", item => Display.DateFromUtc(item.CreatedAt), 90, sort: item => item.CreatedAt)
            .Add("Trạng thái", StatusText, 170,
                color: item => item.IsHidden ? Theme.Muted : item.IsReported ? Theme.Danger : Theme.Success);

        _search = Tool(Ui.SearchBox("Tìm theo khách, sân, nội dung", ReloadAsync, 220));
        _filter = FilterCombo(new[]
        {
            new Option<string>("all", "Tất cả"),
            new Option<string>("reported", "Báo cáo vi phạm"),
            new Option<string>("hidden", "Đã bị ẩn"),
            new Option<string>("visible", "Đang hiển thị")
        }, "all", 150);
        AddCommand("Tìm kiếm", ReloadAsync);
        AddAction("Ẩn đánh giá", item => PromptAsync(
            $"Ẩn đánh giá của {item.UserName}",
            "Lý do ẩn",
            "Ẩn đánh giá",
            reason => App.CallAsync((IReviewService service) => service.HideReviewAsync(item.Id, reason)),
            suggestions: HideReasons,
            note: $"{item.CourtName}: \"{item.Comment}\""),
            item => !item.IsHidden);
        AddAction("Khôi phục", item => ExecuteAsync(
            "Khôi phục hiển thị lại đánh giá này?",
            () => App.CallAsync((IReviewService service) => service.UnhideReviewAsync(item.Id))),
            item => item.IsHidden);
        AddAction("Xóa vĩnh viễn", item => ExecuteAsync(
            "Bạn có chắc chắn muốn xóa vĩnh viễn đánh giá này?",
            () => App.CallAsync((IReviewService service) => service.DeleteReviewAsync(item.Id))), danger: true);

        Body.Controls.Add(GridCard(Grid));
        Body.Controls.Add(Gap());
        Body.Controls.Add(StatCard.Row(_total, _visible, _reported, _hidden));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var search = _search.Text;
        var filter = Ui.Value<string>(_filter);
        var (reviews, stats) = await App.CallAsync(async (IReviewService service) =>
            (await service.GetAllReviewsAsync(search, filter), await service.GetReviewStatsAsync()));
        if (!isCurrent())
        {
            return;
        }

        _total.Set(stats.TotalReviews.ToString());
        _visible.Set(stats.VisibleReviews.ToString());
        _reported.Set(stats.ReportedReviews.ToString());
        _hidden.Set(stats.HiddenReviews.ToString());
        Grid.SetItems(reviews);
    }

    private static string StatusText(ReviewItemDto review)
    {
        if (!review.IsHidden)
        {
            return review.IsReported ? "Bị báo cáo" : "Đang hiển thị";
        }

        var hiddenAt = review.HiddenAt is { } time ? $" ({Display.DateTimeFromUtc(time)})" : string.Empty;
        return $"Đã ẩn{hiddenAt}{Environment.NewLine}{review.HiddenReason}".TrimEnd();
    }
}
