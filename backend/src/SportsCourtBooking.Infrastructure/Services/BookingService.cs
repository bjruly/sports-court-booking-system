using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BookingDto>> GetBookingsByDateAsync(DateOnly date, int? courtId = null)
    {
        var query = _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Court)
            .Where(b => b.BookingDate == date && b.Status != BookingStatus.Cancelled);

        if (courtId.HasValue)
        {
            query = query.Where(b => b.CourtId == courtId.Value);
        }

        return await query.OrderBy(b => b.StartTime).Select(b => new BookingDto
        {
            Id = b.Id,
            UserId = b.UserId,
            CustomerName = b.User != null ? b.User.FullName : string.Empty,
            CourtId = b.CourtId,
            CourtName = b.Court != null ? b.Court.Name : string.Empty,
            BookingDate = b.BookingDate,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            TotalPrice = b.TotalPrice,
            Status = b.Status
        }).ToListAsync();
    }

    public async Task<bool> IsTimeSlotAvailableAsync(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        // Hai khoảng thời gian [StartA, EndA] và [StartB, EndB] giao nhau khi: StartA < EndB VÀ EndA > StartB
        bool isOverlapping = await _context.Bookings.AnyAsync(b =>
            b.CourtId == courtId &&
            b.BookingDate == date &&
            b.Status != BookingStatus.Cancelled &&
            startTime < b.EndTime &&
            endTime > b.StartTime);

        return !isOverlapping;
    }

    public async Task<(bool Success, string Message, BookingDto? Data)> CreateBookingAsync(CreateBookingRequest request)
    {
        if (request.EndTime <= request.StartTime)
            return (false, "Giờ kết thúc phải lớn hơn giờ bắt đầu.", null);

        var court = await _context.Courts.FindAsync(request.CourtId);
        if (court == null || court.Status != CourtStatus.Active)
            return (false, "Sân không tồn tại hoặc đang tạm ngưng hoạt động.", null);

        bool isAvailable = await IsTimeSlotAvailableAsync(request.CourtId, request.BookingDate, request.StartTime, request.EndTime);
        if (!isAvailable)
            return (false, "Khung giờ này đã có người đặt. Vui lòng chọn khung giờ khác!", null);

        // Tính tổng số giờ thuê và thành tiền
        double totalHours = (request.EndTime - request.StartTime).TotalHours;
        decimal totalPrice = Math.Round((decimal)totalHours * court.PricePerHour, 0);

        var booking = new Booking
        {
            UserId = request.UserId,
            CourtId = request.CourtId,
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalPrice = totalPrice,
            Note = request.Note,
            Status = BookingStatus.Confirmed
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return (true, "Đặt sân thành công!", new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            CourtId = booking.CourtId,
            CourtName = court.Name,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status
        });
    }
}