using Microsoft.Extensions.DependencyInjection;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;

namespace SportsCourtBooking.Desktop.Core;

/// <summary>Tài khoản đang đăng nhập đã bị quản trị viên khóa.</summary>
public sealed class AccountLockedException : Exception
{
    public AccountLockedException()
        : base("Tài khoản của bạn đang bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.")
    {
    }
}

/// <summary>
/// Cầu nối giữa giao diện và lớp nghiệp vụ. Mỗi lần gọi chạy trong một DI scope riêng (một DbContext riêng)
/// và ngoài luồng giao diện, để cửa sổ không bị đứng.
/// </summary>
public sealed class AppServices
{
    private readonly IServiceProvider _provider;

    public AppServices(IServiceProvider provider) => _provider = provider;

    public UserDto? CurrentUser { get; private set; }

    public int UserId => CurrentUser?.Id ?? throw new InvalidOperationException("Chưa đăng nhập.");

    public void SignIn(UserDto user) => CurrentUser = user;

    public void SignOut() => CurrentUser = null;

    public Task<TResult> CallAsync<TService, TResult>(Func<TService, Task<TResult>> action)
        where TService : notnull
    {
        var userId = CurrentUser?.Id;
        return Task.Run(async () =>
        {
            await using var scope = _provider.CreateAsyncScope();
            if (userId.HasValue
                && !await scope.ServiceProvider.GetRequiredService<IUserService>().IsUserActiveAsync(userId.Value))
            {
                throw new AccountLockedException();
            }

            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        });
    }
}
