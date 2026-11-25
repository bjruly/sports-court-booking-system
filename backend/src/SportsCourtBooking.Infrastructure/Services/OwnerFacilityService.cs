using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;
using Npgsql;

namespace SportsCourtBooking.Infrastructure.Services;

public class OwnerFacilityService : IOwnerFacilityService
{
    private readonly AppDbContext _context;

    public OwnerFacilityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OwnerFacilityDto>> GetOwnerFacilitiesAsync(int ownerId)
        => await _context.Facilities
            .AsNoTracking()
            .Where(facility => facility.OwnerId == ownerId && facility.Owner!.Role == UserRole.CourtOwner)
            .OrderByDescending(facility => facility.CreatedAt)
            .Select(facility => new OwnerFacilityDto
            {
                Id = facility.Id,
                Name = facility.Name,
                Address = facility.Address,
                District = facility.Courts.OrderBy(c => c.Id).Select(c => c.District).FirstOrDefault(),
                PhoneNumber = facility.PhoneNumber,
                OpenTime = facility.OpenTime,
                CloseTime = facility.CloseTime,
                Description = facility.Description,
                IsApproved = facility.IsApproved,
                IsActive = facility.IsActive,
                CourtCount = facility.Courts.Count,
                ActiveCourtCount = facility.Courts.Count(c => c.Status == CourtStatus.Active),
                MaintenanceCourtCount = facility.Courts.Count(c => c.Status == CourtStatus.Maintenance),
                CourtTypeId = facility.Courts.Select(c => (int?)c.CourtTypeId).FirstOrDefault(),
                CourtTypeName = facility.Courts.Select(c => c.CourtType != null ? c.CourtType.Name : null).FirstOrDefault()
            })
            .ToListAsync();

    public async Task<OwnerFacilityDto?> GetOwnerFacilityAsync(int facilityId, int ownerId)
        => await _context.Facilities
            .AsNoTracking()
            .Where(facility => facility.Id == facilityId
                && facility.OwnerId == ownerId
                && facility.Owner!.Role == UserRole.CourtOwner)
            .Select(facility => new OwnerFacilityDto
            {
                Id = facility.Id,
                Name = facility.Name,
                Address = facility.Address,
                District = facility.Courts.OrderBy(c => c.Id).Select(c => c.District).FirstOrDefault(),
                PhoneNumber = facility.PhoneNumber,
                OpenTime = facility.OpenTime,
                CloseTime = facility.CloseTime,
                Description = facility.Description,
                IsApproved = facility.IsApproved,
                IsActive = facility.IsActive,
                CourtCount = facility.Courts.Count,
                ActiveCourtCount = facility.Courts.Count(c => c.Status == CourtStatus.Active),
                MaintenanceCourtCount = facility.Courts.Count(c => c.Status == CourtStatus.Maintenance),
                CourtTypeId = facility.Courts.Select(c => (int?)c.CourtTypeId).FirstOrDefault(),
                CourtTypeName = facility.Courts.Select(c => c.CourtType != null ? c.CourtType.Name : null).FirstOrDefault()
            })
            .SingleOrDefaultAsync();

    public async Task<(bool Success, string Message)> CreateOwnerFacilityAsync(
        int ownerId,
        OwnerFacilityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
        {
            return (false, "Tên cơ sở phải từ 2 đến 150 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return (false, "Địa chỉ cơ sở không được để trống.");
        }

        var isOwner = await _context.Users
            .AnyAsync(user => user.Id == ownerId && user.Role == UserRole.CourtOwner);
        if (!isOwner)
        {
            return (false, "Không tìm thấy tài khoản Chủ sân.");
        }

        var courtType = await _context.CourtTypes.FindAsync(request.CourtTypeId);
        if (courtType == null)
        {
            return (false, "Vui lòng chọn 1 môn thể thao hợp lệ cho cơ sở.");
        }

        var courtCount = request.NumberOfCourts;
        if (courtCount < 1 || courtCount > 30)
        {
            return (false, "Số lượng sân đăng ký phải từ 1 đến 30 sân.");
        }

        if (request.PricePerHour < 10000 || request.PricePerHour > 50000000)
        {
            return (false, "Giá thuê mỗi giờ phải từ 10.000 đ đến 50.000.000 đ.");
        }

        var district = string.IsNullOrWhiteSpace(request.District) ? "Quận 10" : request.District.Trim();
        var openTime = string.IsNullOrWhiteSpace(request.OpenTime) ? "06:00" : request.OpenTime.Trim();
        var closeTime = string.IsNullOrWhiteSpace(request.CloseTime) ? "22:00" : request.CloseTime.Trim();
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? "" : request.PhoneNumber.Trim();

        var facility = new Facility
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            PhoneNumber = phone,
            OpenTime = openTime,
            CloseTime = closeTime,
            Description = request.Description?.Trim(),
            IsApproved = false, // Chờ Admin duyệt theo yêu cầu
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Facilities.Add(facility);
        await _context.SaveChangesAsync();

        // Tự động khởi tạo số lượng sân đăng ký thuộc môn thể thao duy nhất này
        var courts = new List<Court>();
        for (int i = 1; i <= courtCount; i++)
        {
            courts.Add(new Court
            {
                OwnerId = ownerId,
                FacilityId = facility.Id,
                CourtTypeId = courtType.Id,
                Name = $"Sân {i} ({courtType.Name})",
                Location = facility.Address,
                District = district,
                PricePerHour = request.PricePerHour,
                PeakStartHour = 17,
                PeakSurcharge = 50000m,
                Status = CourtStatus.Inactive, // Chưa hoạt động cho đến khi Admin phê duyệt cơ sở
                Description = $"Sân thi đấu {courtType.Name} tiêu chuẩn tại {facility.Name}.",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.Courts.AddRangeAsync(courts);
        await _context.SaveChangesAsync();

        return (true, $"Đã gửi hồ sơ đăng ký cơ sở \"{facility.Name}\" với {courtCount} sân môn {courtType.Name}. Vui lòng chờ Quản trị viên duyệt!");
    }

    public async Task<(bool Success, string Message)> UpdateOwnerFacilityAsync(
        int facilityId,
        int ownerId,
        OwnerFacilityRequest request)
    {
        var facility = await _context.Facilities
            .Include(f => f.Courts)
            .SingleOrDefaultAsync(item => item.Id == facilityId
                && item.OwnerId == ownerId
                && item.Owner!.Role == UserRole.CourtOwner);
        if (facility is null)
        {
            return (false, "Không tìm thấy cơ sở thuộc tài khoản của bạn.");
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
        {
            return (false, "Tên cơ sở phải từ 2 đến 150 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return (false, "Địa chỉ cơ sở không được để trống.");
        }

        // Kiểm tra xem có thay đổi thông tin quan trọng cần Admin duyệt lại không
        var newName = request.Name.Trim();
        var newAddress = request.Address.Trim();
        var newOpenTime = string.IsNullOrWhiteSpace(request.OpenTime) ? "06:00" : request.OpenTime.Trim();
        var newCloseTime = string.IsNullOrWhiteSpace(request.CloseTime) ? "22:00" : request.CloseTime.Trim();

        bool needsReApproval = facility.IsApproved && (
            facility.Name != newName ||
            facility.Address != newAddress ||
            facility.OpenTime != newOpenTime ||
            facility.CloseTime != newCloseTime
        );

        facility.Name = newName;
        facility.Address = newAddress;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            facility.PhoneNumber = request.PhoneNumber.Trim();
        facility.OpenTime = newOpenTime;
        facility.CloseTime = newCloseTime;
        facility.Description = request.Description?.Trim();
        facility.UpdatedAt = DateTime.UtcNow;

        if (needsReApproval)
        {
            facility.IsApproved = false;
            // Tạm ngưng các sân cho đến khi Admin duyệt lại
            foreach (var court in facility.Courts)
            {
                court.Status = CourtStatus.Inactive;
            }
        }

        // Chỉ ghi quận/huyện xuống các sân khi chủ sân thực sự đổi, để không mất giá trị riêng của từng sân.
        var currentDistrict = facility.Courts.OrderBy(court => court.Id).FirstOrDefault()?.District;
        var newDistrict = request.District?.Trim();
        var districtChanged = !string.IsNullOrEmpty(newDistrict) && newDistrict != currentDistrict;
        foreach (var court in facility.Courts)
        {
            court.Location = facility.Address;
            if (districtChanged)
            {
                court.District = newDistrict!;
            }
        }

        await _context.SaveChangesAsync();

        if (needsReApproval)
        {
            return (true, "Đã cập nhật thông tin cơ sở. Do thay đổi tên/địa chỉ/giờ mở cửa nên cơ sở sẽ chờ Admin duyệt lại.");
        }
        return (true, "Đã cập nhật thông tin cơ sở.");
    }

    public async Task<(bool Success, string Message)> DeleteOwnerFacilityAsync(int facilityId, int ownerId)
    {
        var facility = await _context.Facilities
            .SingleOrDefaultAsync(item => item.Id == facilityId
                && item.OwnerId == ownerId
                && item.Owner!.Role == UserRole.CourtOwner);
        if (facility is null)
        {
            return (false, "Không tìm thấy cơ sở thuộc tài khoản của bạn.");
        }

        if (await _context.Courts.AnyAsync(court => court.FacilityId == facilityId))
        {
            return (false, "Không thể xóa cơ sở đang có sân. Hãy chuyển các sân sang cơ sở khác trước.");
        }

        _context.Facilities.Remove(facility);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ForeignKeyViolation
            })
        {
            _context.Entry(facility).State = EntityState.Unchanged;
            return (false, "Không thể xóa cơ sở đang có sân. Hãy chuyển các sân sang cơ sở khác trước.");
        }

        return (true, "Đã xóa cơ sở.");
    }

    public async Task<(bool Success, string Message)> AddCourtToFacilityAsync(int facilityId, int ownerId)
    {
        var facility = await _context.Facilities
            .Include(f => f.Courts)
            .SingleOrDefaultAsync(f => f.Id == facilityId
                && f.OwnerId == ownerId
                && f.Owner!.Role == UserRole.CourtOwner);

        if (facility is null)
        {
            return (false, "Không tìm thấy cơ sở thuộc tài khoản của bạn.");
        }

        if (facility.Courts.Count >= 30)
        {
            return (false, "Cơ sở đã đạt giới hạn tối đa 30 sân.");
        }

        // Lấy loại sân từ sân đầu tiên trong cơ sở
        var firstCourt = facility.Courts.OrderBy(c => c.Id).FirstOrDefault();
        int courtTypeId = firstCourt?.CourtTypeId ?? 1;
        string courtTypeName = firstCourt != null
            ? (await _context.CourtTypes.FindAsync(courtTypeId))?.Name ?? "Cầu lông"
            : "Cầu lông";
        decimal price = firstCourt?.PricePerHour ?? 80000m;
        string district = firstCourt?.District ?? "Quận 10";

        int newCourtNumber = facility.Courts.Count + 1;

        // Nếu cơ sở đã được Admin duyệt → sân mới Active ngay. Chưa duyệt → Inactive
        var status = (facility.IsApproved && facility.IsActive)
            ? CourtStatus.Active
            : CourtStatus.Inactive;

        var court = new Court
        {
            OwnerId = ownerId,
            FacilityId = facility.Id,
            CourtTypeId = courtTypeId,
            Name = $"Sân {newCourtNumber} ({courtTypeName})",
            Location = facility.Address,
            District = district,
            PricePerHour = price,
            PeakStartHour = 17,
            PeakSurcharge = 50000m,
            Status = status,
            Description = $"Sân thi đấu {courtTypeName} tiêu chuẩn tại {facility.Name}.",
            CreatedAt = DateTime.UtcNow
        };

        _context.Courts.Add(court);
        await _context.SaveChangesAsync();

        var statusMsg = status == CourtStatus.Active ? "hoạt động ngay" : "chờ cơ sở được duyệt";
        return (true, $"Đã thêm \"{court.Name}\" vào cơ sở \"{facility.Name}\" ({statusMsg}).");
    }

    private static bool TryNormalize(
        OwnerFacilityRequest request,
        out string name,
        out string address,
        out string? description)
    {
        name = request.Name?.Trim() ?? string.Empty;
        address = request.Address?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        return name.Length is >= 2 and <= 150
            && address.Length is > 0 and <= 255
            && (description is null || description.Length <= 1000);
    }
}
