using SportsCourtBooking.Application.DTOs;

namespace SportsCourtBooking.Application.Interfaces;

public interface IAccountService
{
    Task<UserDto?> GetProfileAsync(int userId);

    Task<(bool Success, string Message)> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request);

    Task<(bool Success, string Message)> ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request);
}