using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminReportDto> GetAdminReportAsync()
    {
        // 1. Thống kê khách hàng
        var customers = await _context.Users
            .Where(u => u.Role == UserRole.Customer)
            .Select(u => new { u.Id, u.IsActive })
            .ToListAsync();

        var totalCustomers = customers.Count;
        var activeCustomers = customers.Count(c => c.IsActive);
        var lockedCustomers = totalCustomers - activeCustomers;

        // 2. Thống kê sân
        var courts = await _context.Courts
            .Select(c => new { c.Id, c.CourtTypeId, c.Status })
            .ToListAsync();

        var totalCourts = courts.Count;
        var activeCourts = courts.Count(c => c.Status == CourtStatus.Active);

        // 3. Thống kê đơn đặt sân
        var bookings = await _context.Bookings
            .Select(b => new { b.Id, b.CourtId, b.Status, b.TotalPrice })
            .ToListAsync();

        var totalBookings = bookings.Count;
        var confirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed);
        var pendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending);
        var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.Expired);

        var totalRevenue = bookings
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
            .Sum(b => b.TotalPrice);

        var pendingRevenue = bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Sum(b => b.TotalPrice);

        // 4. Báo cáo tổng hợp theo môn / loại sân
        var courtTypes = await _context.CourtTypes.OrderBy(ct => ct.Id).ToListAsync();
        var courtTypeReports = courtTypes.Select(ct =>
        {
            var ctCourtIds = courts.Where(c => c.CourtTypeId == ct.Id).Select(c => c.Id).ToHashSet();
            var ctBookings = bookings.Where(b => ctCourtIds.Contains(b.CourtId)).ToList();
            var successfulCtBookings = ctBookings.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed).ToList();
            var ctRevenue = successfulCtBookings.Sum(b => b.TotalPrice);

            return new CourtTypeReportDto
            {
                CourtTypeId = ct.Id,
                CourtTypeName = ct.Name,
                CourtCount = ctCourtIds.Count,
                TotalBookings = ctBookings.Count,
                SuccessfulBookings = successfulCtBookings.Count,
                Revenue = ctRevenue,
                ContributionPercent = totalRevenue > 0 ? Math.Round((double)(ctRevenue / totalRevenue * 100), 1) : 0
            };
        }).ToList();

        // 5. Báo cáo tổng hợp theo trạng thái đơn
        var statusDisplay = new Dictionary<BookingStatus, string>
        {
            { BookingStatus.Pending, "Chờ duyệt / Giữ chỗ" },
            { BookingStatus.Confirmed, "Đã xác nhận" },
            { BookingStatus.Completed, "Đã hoàn thành" },
            { BookingStatus.Cancelled, "Đã hủy" },
            { BookingStatus.Expired, "Hết hạn giữ chỗ" }
        };

        var bookingStatusReports = Enum.GetValues<BookingStatus>().Select(status =>
        {
            var statusBookings = bookings.Where(b => b.Status == status).ToList();
            var count = statusBookings.Count;
            var amount = statusBookings.Sum(b => b.TotalPrice);
            var percent = totalBookings > 0 ? Math.Round((double)count / totalBookings * 100, 1) : 0;

            return new BookingStatusReportDto
            {
                Status = status,
                StatusName = statusDisplay.TryGetValue(status, out var name) ? name : status.ToString(),
                Count = count,
                TotalAmount = amount,
                Percent = percent
            };
        }).ToList();

        return new AdminReportDto
        {
            TotalRevenue = totalRevenue,
            PendingRevenue = pendingRevenue,
            TotalBookings = totalBookings,
            ConfirmedBookings = confirmedBookings,
            PendingBookings = pendingBookings,
            CancelledBookings = cancelledBookings,
            TotalCustomers = totalCustomers,
            ActiveCustomers = activeCustomers,
            LockedCustomers = lockedCustomers,
            TotalCourts = totalCourts,
            ActiveCourts = activeCourts,
            CourtTypeReports = courtTypeReports,
            BookingStatusReports = bookingStatusReports
        };
    }

    public async Task<List<CategoryDetailReportDto>> GetCategoryReportsAsync()
    {
        var courtTypes = await _context.CourtTypes
            .Include(ct => ct.Courts)
            .OrderBy(ct => ct.Id)
            .ToListAsync();

        var bookings = await _context.Bookings
            .Select(b => new { b.Id, b.CourtId, b.Status, b.TotalPrice })
            .ToListAsync();

        var totalSystemRevenue = bookings
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
            .Sum(b => b.TotalPrice);

        var result = new List<CategoryDetailReportDto>();

        foreach (var ct in courtTypes)
        {
            var courts = ct.Courts.ToList();
            var courtIds = courts.Select(c => c.Id).ToHashSet();
            var ctBookings = bookings.Where(b => courtIds.Contains(b.CourtId)).ToList();

            var successfulBookings = ctBookings.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed).ToList();
            var pendingBookings = ctBookings.Where(b => b.Status == BookingStatus.Pending).ToList();
            var cancelledBookings = ctBookings.Where(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.Expired).ToList();

            var revenue = successfulBookings.Sum(b => b.TotalPrice);
            var pendingRevenue = pendingBookings.Sum(b => b.TotalPrice);
            var totalBookingsCount = ctBookings.Count;

            var courtItems = courts.Select(c =>
            {
                var cBookings = ctBookings.Where(b => b.CourtId == c.Id).ToList();
                var cSuccessful = cBookings.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed).ToList();
                return new CategoryCourtItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Location = c.Location,
                    PricePerHour = c.PricePerHour,
                    Status = c.Status,
                    TotalBookings = cBookings.Count,
                    Revenue = cSuccessful.Sum(b => b.TotalPrice)
                };
            }).ToList();

            result.Add(new CategoryDetailReportDto
            {
                CourtTypeId = ct.Id,
                CourtTypeName = ct.Name,
                Description = ct.Description ?? string.Empty,
                TotalCourts = courts.Count,
                ActiveCourts = courts.Count(c => c.Status == CourtStatus.Active),
                MinPricePerHour = courts.Any() ? courts.Min(c => c.PricePerHour) : 0,
                MaxPricePerHour = courts.Any() ? courts.Max(c => c.PricePerHour) : 0,
                TotalBookings = totalBookingsCount,
                SuccessfulBookings = successfulBookings.Count,
                PendingBookings = pendingBookings.Count,
                CancelledBookings = cancelledBookings.Count,
                SuccessRate = totalBookingsCount > 0 ? Math.Round((double)successfulBookings.Count / totalBookingsCount * 100, 1) : 0,
                Revenue = revenue,
                PendingRevenue = pendingRevenue,
                ContributionPercent = totalSystemRevenue > 0 ? Math.Round((double)(revenue / totalSystemRevenue * 100), 1) : 0,
                AverageOrderValue = successfulBookings.Count > 0 ? Math.Round(revenue / successfulBookings.Count, 0) : 0,
                Courts = courtItems
            });
        }

        return result;
    }
}
