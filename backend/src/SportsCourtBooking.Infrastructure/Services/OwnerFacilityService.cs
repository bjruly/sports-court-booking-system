using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

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
            .OrderBy(facility => facility.Name)
            .Select(facility => new OwnerFacilityDto
            {
                Id = facility.Id,
                Name = facility.Name,
                Address = facility.Address,
                Description = facility.Description,
                CourtCount = facility.Courts.Count
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
                Description = facility.Description,
                CourtCount = facility.Courts.Count
            })
            .SingleOrDefaultAsync();

    public async Task<(bool Success, string Message)> CreateOwnerFacilityAsync(
        int ownerId,
        OwnerFacilityRequest request)
    {
        if (!TryNormalize(request, out var name, out var address, out var description))
        {
            return (false, "Thông tin cơ sở không hợp lệ.");
        }

        var isOwner = await _context.Users
            .AnyAsync(user => user.Id == ownerId && user.Role == UserRole.CourtOwner);
        if (!isOwner)
        {
            return (false, "Không tìm thấy tài khoản Chủ sân.");
        }

        _context.Facilities.Add(new Facility
        {
            OwnerId = ownerId,
            Name = name,
            Address = address,
            Description = description
        });
        await _context.SaveChangesAsync();

        return (true, "Đã thêm cơ sở thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateOwnerFacilityAsync(
        int facilityId,
        int ownerId,
        OwnerFacilityRequest request)
    {
        var facility = await _context.Facilities
            .SingleOrDefaultAsync(item => item.Id == facilityId
                && item.OwnerId == ownerId
                && item.Owner!.Role == UserRole.CourtOwner);
        if (facility is null)
        {
            return (false, "Không tìm thấy cơ sở thuộc tài khoản của bạn.");
        }

        if (!TryNormalize(request, out var name, out var address, out var description))
        {
            return (false, "Thông tin cơ sở không hợp lệ.");
        }

        facility.Name = name;
        facility.Address = address;
        facility.Description = description;
        facility.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Đã cập nhật thông tin cơ sở.");
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
