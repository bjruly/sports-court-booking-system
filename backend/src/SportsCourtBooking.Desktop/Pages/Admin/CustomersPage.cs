using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class CustomersPage : ListPage<UserDto>
{
    private readonly TextBox _search;

    public CustomersPage(AppServices app, IShell shell)
        : base(app, shell, "Khách hàng", "Tra cứu, khóa và mở khóa tài khoản khách hàng.", item => item.Id)
    {
        Grid.EmptyText = "Không tìm thấy khách hàng nào.";
        Grid.Add("Họ và tên", item => item.FullName, 160, bold: true)
            .Add("Email", item => item.Email, 190)
            .Add("Số điện thoại", item => item.PhoneNumber, 110)
            .Add("Ngày đăng ký", item => Display.DateFromUtc(item.CreatedAt), 100, sort: item => item.CreatedAt)
            .Add("Trạng thái", item => item.IsActive ? "Hoạt động" : "Đã khóa", 100,
                color: item => item.IsActive ? Theme.Success : Theme.Danger);

        _search = Tool(Ui.SearchBox("Tìm theo tên, email, số điện thoại", ReloadAsync));
        AddCommand("Tìm kiếm", ReloadAsync);
        AddAction("Khóa tài khoản", item => ToggleAsync(item, $"Bạn có chắc muốn KHÓA tài khoản của {item.FullName}?"),
            item => item.IsActive, danger: true);
        AddAction("Mở khóa", item => ToggleAsync(item, $"Bạn có chắc muốn MỞ KHÓA tài khoản của {item.FullName}?"),
            item => !item.IsActive);
        Body.Controls.Add(GridCard(Grid));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var search = _search.Text;
        var customers = await App.CallAsync((IUserService service) => service.GetCustomersAsync(search));
        if (isCurrent())
        {
            Grid.SetItems(customers);
        }
    }

    private Task ToggleAsync(UserDto customer, string question) => ExecuteAsync(question, async () =>
    {
        var result = await App.CallAsync((IUserService service) => service.ToggleLockAsync(customer.Id, App.UserId));
        return (result.Success, result.Message);
    });
}
