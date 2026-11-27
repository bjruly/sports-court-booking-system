using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync(string? search = null)
    {
        var query = _context.CourtTypes
            .Include(ct => ct.Courts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(ct =>
                ct.Name.ToLower().Contains(keyword) ||
                (ct.Description != null && ct.Description.ToLower().Contains(keyword)));
        }

        return await query
            .OrderBy(ct => ct.Id)
            .Select(ct => new CategoryDto
            {
                Id = ct.Id,
                Name = ct.Name,
                Description = ct.Description,
                CourtCount = ct.Courts.Count,
                CreatedAt = ct.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        return await _context.CourtTypes
            .Where(ct => ct.Id == id)
            .Select(ct => new CategoryDto
            {
                Id = ct.Id,
                Name = ct.Name,
                Description = ct.Description,
                CourtCount = ct.Courts.Count,
                CreatedAt = ct.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, CategoryDto? Data)> CreateCategoryAsync(CreateCategoryRequest request, int? adminUserId = null)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Tên danh mục không được để trống.", null);

        var exists = await _context.CourtTypes
            .AnyAsync(ct => ct.Name.ToLower() == name.ToLower());

        if (exists)
            return (false, $"Danh mục \"{name}\" đã tồn tại.", null);

        var courtType = new CourtType
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.CourtTypes.Add(courtType);
        _context.RecordAdminActivity(adminUserId, "Thêm danh mục", courtType.Name);
        await _context.SaveChangesAsync();

        var dto = new CategoryDto
        {
            Id = courtType.Id,
            Name = courtType.Name,
            Description = courtType.Description,
            CourtCount = 0,
            CreatedAt = courtType.CreatedAt
        };

        return (true, $"Thêm danh mục \"{name}\" thành công!", dto);
    }

    public async Task<(bool Success, string Message)> UpdateCategoryAsync(UpdateCategoryRequest request, int? adminUserId = null)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Tên danh mục không được để trống.");

        var courtType = await _context.CourtTypes.FindAsync(request.Id);
        if (courtType == null)
            return (false, "Không tìm thấy danh mục cần cập nhật.");

        var duplicate = await _context.CourtTypes
            .AnyAsync(ct => ct.Id != request.Id && ct.Name.ToLower() == name.ToLower());

        if (duplicate)
            return (false, $"Tên danh mục \"{name}\" đã được sử dụng bởi danh mục khác.");

        courtType.Name = name;
        courtType.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        courtType.UpdatedAt = DateTime.UtcNow;
        _context.RecordAdminActivity(adminUserId, "Sửa danh mục", $"Danh mục #{courtType.Id} · {courtType.Name}");

        await _context.SaveChangesAsync();
        return (true, $"Cập nhật danh mục \"{name}\" thành công!");
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id, int? adminUserId = null)
    {
        var courtType = await _context.CourtTypes
            .Include(ct => ct.Courts)
            .FirstOrDefaultAsync(ct => ct.Id == id);

        if (courtType == null)
            return (false, "Không tìm thấy danh mục cần xóa.");

        if (courtType.Courts.Any())
        {
            return (false, $"Không thể xóa danh mục \"{courtType.Name}\" vì hiện có {courtType.Courts.Count} sân thể thao trực thuộc.");
        }

        _context.CourtTypes.Remove(courtType);
        _context.RecordAdminActivity(adminUserId, "Xóa danh mục", $"Danh mục #{courtType.Id} · {courtType.Name}");
        await _context.SaveChangesAsync();

        return (true, $"Xóa danh mục \"{courtType.Name}\" thành công!");
    }
}
