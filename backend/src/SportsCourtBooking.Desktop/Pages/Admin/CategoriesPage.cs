using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Desktop.Dialogs;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class CategoriesPage : ListPage<CategoryDto>
{
    private readonly TextBox _search;

    public CategoriesPage(AppServices app, IShell shell)
        : base(app, shell, "Môn thể thao & loại sân", "Quản lý danh mục môn thể thao.", item => item.Id)
    {
        Grid.EmptyText = "Không tìm thấy danh mục nào.";
        Grid.Add("Tên danh mục", item => item.Name, 160, bold: true)
            .Add("Mô tả", item => item.Description ?? string.Empty, 320)
            .Add("Số sân", item => item.CourtCount.ToString(), 70, right: true, sort: item => item.CourtCount)
            .Add("Ngày tạo", item => Display.DateFromUtc(item.CreatedAt), 100, sort: item => item.CreatedAt);

        _search = Tool(Ui.SearchBox("Tìm danh mục", ReloadAsync));
        AddCommand("Tìm kiếm", ReloadAsync);
        AddCommand("+ Thêm danh mục", () => EditAsync(null), primary: true);
        AddAction("Sửa", EditAsync);
        AddAction("Xóa", item => ExecuteAsync(
            $"Bạn có chắc muốn xóa danh mục \"{item.Name}\"?",
            () => App.CallAsync((ICategoryService service) => service.DeleteCategoryAsync(item.Id))), danger: true);
        Body.Controls.Add(GridCard(Grid));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var search = _search.Text;
        var categories = await App.CallAsync((ICategoryService service) => service.GetAllCategoriesAsync(search));
        if (isCurrent())
        {
            Grid.SetItems(categories);
        }
    }

    private Task EditAsync(CategoryDto? category)
    {
        var dialog = new FormDialog(category is null ? "Thêm danh mục" : "Sửa danh mục");
        var name = dialog.AddText("Tên danh mục", category?.Name, 100);
        var description = dialog.AddText("Mô tả", category?.Description, 500, 4);
        dialog.OnSubmit(async () =>
        {
            if (category is null)
            {
                var create = new CreateCategoryRequest { Name = name.Text, Description = description.Text };
                if (Validation.Error(create) is { } createError)
                {
                    return (false, createError);
                }

                var created = await App.CallAsync((ICategoryService service) => service.CreateCategoryAsync(create));
                return (created.Success, created.Message);
            }

            var update = new UpdateCategoryRequest { Id = category.Id, Name = name.Text, Description = description.Text };
            return Validation.Error(update) is { } updateError
                ? (false, updateError)
                : await App.CallAsync((ICategoryService service) => service.UpdateCategoryAsync(update));
        });
        return ShowDialogAsync(dialog);
    }
}
