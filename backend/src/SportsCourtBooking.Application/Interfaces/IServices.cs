using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Application.Interfaces;

public interface ICourtService
{
    Task<List<CourtDto>> GetAllCourtsAsync(int? courtTypeId = null);
    Task<CourtDto?> GetCourtByIdAsync(int id);
}

public interface IBookingService
{
    Task<List<BookingDto>> GetBookingsByDateAsync(DateOnly date, int? courtId = null);
    Task<bool> IsTimeSlotAvailableAsync(int courtId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
    Task<(bool Success, string Message, BookingDto? Data)> CreateBookingAsync(CreateBookingRequest request);
}

public interface IAuthService
{
    Task<(bool Success, string Message, UserDto? User)> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message, UserDto? User)> RegisterAsync(RegisterRequest request);
}