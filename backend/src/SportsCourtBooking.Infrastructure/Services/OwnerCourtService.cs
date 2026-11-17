using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class OwnerCourtService : IOwnerCourtService
{
    private readonly AppDbContext _context;

    public OwnerCourtService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CourtDto>> GetOwnerCourtsAsync(int ownerId)
    {
        return await _context.Courts
            .AsNoTracking()
            .Where(court => court.OwnerId == ownerId)
            .OrderBy(court => court.Name)
            .Select(court => new CourtDto
            {
                Id = court.Id,
                Name = court.Name,
                CourtTypeId = court.CourtTypeId,
                CourtTypeName = court.CourtType!.Name,
                Location = court.Location,
                Description = court.Description,
                ImageUrl = court.ImageUrl,
                PricePerHour = court.PricePerHour,
                Status = court.Status
            })
            .ToListAsync();
    }

    public async Task<List<CourtTypeDto>> GetCourtTypesAsync()
    {
        return await _context.CourtTypes
            .AsNoTracking()
            .OrderBy(courtType => courtType.Name)
            .Select(courtType => new CourtTypeDto
            {
                Id = courtType.Id,
                Name = courtType.Name
            })
            .ToListAsync();
    }

    public async Task<CourtDto?> GetOwnerCourtAsync(int courtId, int ownerId)
    {
        return await _context.Courts
            .AsNoTracking()
            .Where(court => court.Id == courtId && court.OwnerId == ownerId)
            .Select(court => new CourtDto
            {
                Id = court.Id,
                Name = court.Name,
                CourtTypeId = court.CourtTypeId,
                CourtTypeName = court.CourtType!.Name,
                Location = court.Location,
                Description = court.Description,
                ImageUrl = court.ImageUrl,
                PricePerHour = court.PricePerHour,
                Status = court.Status
            })
            .SingleOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> CreateOwnerCourtAsync(int ownerId, OwnerCourtRequest request)
    {
        if (!await IsValidRequestAsync(request) || request.PricePerHour is not decimal pricePerHour)
        {
            return (false, "Thông tin sân hoặc môn thể thao không hợp lệ.");
        }

        _context.Courts.Add(new Court
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            CourtTypeId = request.CourtTypeId,
            Location = request.Location.Trim(),
            Description = NormalizeOptional(request.Description),
            ImageUrl = NormalizeOptional(request.ImageUrl),
            PricePerHour = pricePerHour,
            Status = request.Status
        });

        await _context.SaveChangesAsync();
        return (true, "Đã thêm sân thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateOwnerCourtAsync(int courtId, int ownerId, OwnerCourtRequest request)
    {
        var court = await _context.Courts
            .SingleOrDefaultAsync(item => item.Id == courtId && item.OwnerId == ownerId);
        if (court is null)
        {
            return (false, "Không tìm thấy sân thuộc tài khoản của bạn.");
        }

        if (!await IsValidRequestAsync(request) || request.PricePerHour is not decimal pricePerHour)
        {
            return (false, "Thông tin sân hoặc môn thể thao không hợp lệ.");
        }

        ApplyRequest(court, request, pricePerHour);
        court.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, "Đã cập nhật thông tin sân.");
    }

    public async Task<(bool Success, string Message)> UpdateOwnerCourtStatusAsync(int courtId, int ownerId, CourtStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            return (false, "Trạng thái sân không hợp lệ.");
        }

        var court = await _context.Courts
            .SingleOrDefaultAsync(item => item.Id == courtId && item.OwnerId == ownerId);
        if (court is null)
        {
            return (false, "Không tìm thấy sân thuộc tài khoản của bạn.");
        }

        court.Status = status;
        court.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, "Đã cập nhật trạng thái sân.");
    }

    private async Task<bool> IsValidRequestAsync(OwnerCourtRequest request)
    {
        return Enum.IsDefined(request.Status)
            && request.PricePerHour.HasValue
            && request.PricePerHour.Value > 0
            && await _context.CourtTypes.AnyAsync(courtType => courtType.Id == request.CourtTypeId);
    }

    private static void ApplyRequest(Court court, OwnerCourtRequest request, decimal pricePerHour)
    {
        court.Name = request.Name.Trim();
        court.CourtTypeId = request.CourtTypeId;
        court.Location = request.Location.Trim();
        court.Description = NormalizeOptional(request.Description);
        court.ImageUrl = NormalizeOptional(request.ImageUrl);
        court.PricePerHour = pricePerHour;
        court.Status = request.Status;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
