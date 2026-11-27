using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class FacilityService : IFacilityService
{
    private readonly AppDbContext _context;

    public FacilityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<FacilityItemDto>> GetAllFacilitiesAsync(string? search = null, string? filter = null)
    {
        var query = _context.Facilities
            .Include(f => f.Courts)
            .AsNoTracking()
            .AsQueryable();

        // 1. Tìm kiếm theo tên cơ sở, địa chỉ, số điện thoại
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(s) ||
                f.Address.ToLower().Contains(s) ||
                f.PhoneNumber.Contains(s));
        }

        // 2. Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = filter.ToLower() switch
            {
                "pending" => query.Where(f => !f.IsApproved),
                "active" => query.Where(f => f.IsApproved && f.IsActive),
                "suspended" => query.Where(f => !f.IsActive),
                _ => query
            };
        }

        return await query
            .OrderByDescending(f => !f.IsApproved) // Đưa cơ sở chờ duyệt lên đầu
            .ThenByDescending(f => f.CreatedAt)
            .Select(f => new FacilityItemDto
            {
                Id = f.Id,
                Name = f.Name,
                Address = f.Address,
                PhoneNumber = f.PhoneNumber,
                Description = f.Description,
                OpenTime = f.OpenTime,
                CloseTime = f.CloseTime,
                ImageUrl = f.ImageUrl,
                IsApproved = f.IsApproved,
                IsActive = f.IsActive,
                SuspensionReason = f.SuspensionReason,
                TotalCourts = f.Courts.Count,
                CreatedAt = f.CreatedAt,
                OwnerName = f.Owner != null ? f.Owner.FullName : null,
                OwnerPhone = f.Owner != null ? f.Owner.PhoneNumber : null,
                SportName = f.Courts.Select(c => c.CourtType != null ? c.CourtType.Name : null).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<FacilityStatsDto> GetFacilityStatsAsync()
    {
        var total = await _context.Facilities.CountAsync();
        var pending = await _context.Facilities.CountAsync(f => !f.IsApproved);
        var active = await _context.Facilities.CountAsync(f => f.IsApproved && f.IsActive);
        var suspended = await _context.Facilities.CountAsync(f => !f.IsActive);

        return new FacilityStatsDto
        {
            TotalFacilities = total,
            ActiveFacilities = active,
            PendingApprovalFacilities = pending,
            SuspendedFacilities = suspended
        };
    }

    public async Task<FacilityItemDto?> GetFacilityByIdAsync(int id)
    {
        var f = await _context.Facilities
            .Include(x => x.Courts)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (f == null) return null;

        return new FacilityItemDto
        {
            Id = f.Id,
            Name = f.Name,
            Address = f.Address,
            PhoneNumber = f.PhoneNumber,
            Description = f.Description,
            OpenTime = f.OpenTime,
            CloseTime = f.CloseTime,
            ImageUrl = f.ImageUrl,
            IsApproved = f.IsApproved,
            IsActive = f.IsActive,
            SuspensionReason = f.SuspensionReason,
            TotalCourts = f.Courts.Count,
            CreatedAt = f.CreatedAt
        };
    }

    public async Task<(bool Success, string Message, FacilityItemDto? Data)> CreateFacilityAsync(CreateFacilityRequest request, int? adminUserId = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên cơ sở không được để trống.", null);

        if (string.IsNullOrWhiteSpace(request.Address))
            return (false, "Địa chỉ cơ sở không được để trống.", null);

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return (false, "Số điện thoại liên hệ không được để trống.", null);

        var facility = new Facility
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Description = request.Description?.Trim(),
            OpenTime = string.IsNullOrWhiteSpace(request.OpenTime) ? "06:00" : request.OpenTime.Trim(),
            CloseTime = string.IsNullOrWhiteSpace(request.CloseTime) ? "22:00" : request.CloseTime.Trim(),
            IsApproved = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Facilities.Add(facility);
        _context.RecordAdminActivity(adminUserId, "Thêm cơ sở", facility.Name);
        await _context.SaveChangesAsync();

        var dto = new FacilityItemDto
        {
            Id = facility.Id,
            Name = facility.Name,
            Address = facility.Address,
            PhoneNumber = facility.PhoneNumber,
            Description = facility.Description,
            OpenTime = facility.OpenTime,
            CloseTime = facility.CloseTime,
            IsApproved = facility.IsApproved,
            IsActive = facility.IsActive,
            TotalCourts = 0,
            CreatedAt = facility.CreatedAt
        };

        return (true, "Thêm mới cơ sở thể thao thành công.", dto);
    }

    public async Task<(bool Success, string Message)> UpdateFacilityAsync(UpdateFacilityRequest request, int? adminUserId = null)
    {
        var facility = await _context.Facilities.FindAsync(request.Id);
        if (facility == null)
            return (false, "Không tìm thấy cơ sở cần cập nhật.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên cơ sở không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Address))
            return (false, "Địa chỉ cơ sở không được để trống.");

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return (false, "Số điện thoại không được để trống.");

        facility.Name = request.Name.Trim();
        facility.Address = request.Address.Trim();
        facility.PhoneNumber = request.PhoneNumber.Trim();
        facility.Description = request.Description?.Trim();
        facility.OpenTime = string.IsNullOrWhiteSpace(request.OpenTime) ? "06:00" : request.OpenTime.Trim();
        facility.CloseTime = string.IsNullOrWhiteSpace(request.CloseTime) ? "22:00" : request.CloseTime.Trim();
        facility.UpdatedAt = DateTime.UtcNow;
        _context.RecordAdminActivity(adminUserId, "Sửa thông tin cơ sở", $"Cơ sở #{facility.Id} · {facility.Name}");

        await _context.SaveChangesAsync();
        return (true, "Cập nhật thông tin cơ sở thành công.");
    }

    public async Task<(bool Success, string Message)> ApproveFacilityAsync(int facilityId, int? adminUserId = null)
    {
        var facility = await _context.Facilities.FindAsync(facilityId);
        if (facility == null)
            return (false, "Không tìm thấy cơ sở.");

        facility.IsApproved = true;
        facility.IsActive = true;
        facility.UpdatedAt = DateTime.UtcNow;

        // Kích hoạt toàn bộ các sân thuộc cơ sở này để khách hàng có thể đặt trên hệ thống
        var facilityCourts = await _context.Courts.Where(c => c.FacilityId == facilityId).ToListAsync();
        if (facilityCourts.Count == 0)
        {
            var defaultCourtType = await _context.CourtTypes.FirstOrDefaultAsync() ?? new CourtType { Id = 1, Name = "Cầu lông" };
            var ownerId = facility.OwnerId ?? await _context.GetDemoOwnerIdAsync();
            var newCourts = new List<Court>
            {
                new()
                {
                    OwnerId = ownerId,
                    FacilityId = facility.Id,
                    CourtTypeId = defaultCourtType.Id,
                    Name = $"Sân 1 ({defaultCourtType.Name})",
                    Location = facility.Address,
                    District = "TP. Hồ Chí Minh",
                    PricePerHour = 80000m,
                    Status = CourtStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    OwnerId = ownerId,
                    FacilityId = facility.Id,
                    CourtTypeId = defaultCourtType.Id,
                    Name = $"Sân 2 ({defaultCourtType.Name})",
                    Location = facility.Address,
                    District = "TP. Hồ Chí Minh",
                    PricePerHour = 80000m,
                    Status = CourtStatus.Active,
                    CreatedAt = DateTime.UtcNow
                }
            };
            await _context.Courts.AddRangeAsync(newCourts);
            facilityCourts = newCourts;
        }
        else
        {
            foreach (var court in facilityCourts)
            {
                court.Status = CourtStatus.Active;
            }
        }

        _context.RecordAdminActivity(adminUserId, "Phê duyệt cơ sở", $"Cơ sở #{facility.Id} · {facility.Name}");
        await _context.SaveChangesAsync();
        return (true, $"Đã phê duyệt cơ sở \"{facility.Name}\" và kích hoạt {facilityCourts.Count} sân thành công.");
    }

    public async Task<(bool Success, string Message, bool? IsActive)> ToggleLockFacilityAsync(int facilityId, string? reason, int? adminUserId = null)
    {
        var facility = await _context.Facilities.FindAsync(facilityId);
        if (facility == null)
            return (false, "Không tìm thấy cơ sở.", null);

        facility.IsActive = !facility.IsActive;
        facility.UpdatedAt = DateTime.UtcNow;

        var facilityCourts = await _context.Courts.Where(c => c.FacilityId == facilityId).ToListAsync();
        if (!facility.IsActive)
        {
            facility.SuspensionReason = string.IsNullOrWhiteSpace(reason)
                ? "Tạm khóa theo quyết định của Quản trị viên hệ thống"
                : reason.Trim();
            foreach (var court in facilityCourts)
            {
                court.Status = CourtStatus.Inactive;
            }
        }
        else
        {
            facility.SuspensionReason = null;
            if (facility.IsApproved)
            {
                foreach (var court in facilityCourts)
                {
                    court.Status = CourtStatus.Active;
                }
            }
        }

        _context.RecordAdminActivity(adminUserId, facility.IsActive ? "Mở khóa cơ sở" : "Khóa cơ sở",
            $"Cơ sở #{facility.Id} · {facility.Name}");
        await _context.SaveChangesAsync();
        var statusStr = facility.IsActive ? "Mở khóa hoạt động" : "Tạm khóa";
        return (true, $"Đã {statusStr} cơ sở \"{facility.Name}\".", facility.IsActive);
    }
}
