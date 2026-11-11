using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class CourtService : ICourtService
{
    private readonly AppDbContext _context;

    public CourtService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CourtDto>> GetAllCourtsAsync(int? courtTypeId = null)
    {
        var query = _context.Courts.Include(c => c.CourtType).AsQueryable();

        if (courtTypeId.HasValue)
        {
            query = query.Where(c => c.CourtTypeId == courtTypeId.Value);
        }

        return await query.Select(c => new CourtDto
        {
            Id = c.Id,
            Name = c.Name,
            CourtTypeId = c.CourtTypeId,
            CourtTypeName = c.CourtType != null ? c.CourtType.Name : string.Empty,
            Location = c.Location,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            PricePerHour = c.PricePerHour,
            Status = c.Status
        }).ToListAsync();
    }

    public async Task<CourtDto?> GetCourtByIdAsync(int id)
    {
        var c = await _context.Courts.Include(x => x.CourtType).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return null;

        return new CourtDto
        {
            Id = c.Id,
            Name = c.Name,
            CourtTypeId = c.CourtTypeId,
            CourtTypeName = c.CourtType?.Name ?? string.Empty,
            Location = c.Location,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            PricePerHour = c.PricePerHour,
            Status = c.Status
        };
    }
}