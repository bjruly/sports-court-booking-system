using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Security;
using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký DbContext và toàn bộ service nghiệp vụ. Dùng chung cho ứng dụng web (khách hàng, chủ sân, nhân viên)
    /// và ứng dụng WinForms (quản trị viên), để hai bên chạy cùng một bộ quy tắc.
    /// </summary>
    public static IServiceCollection AddSportsCourtServices(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ICourtService, CourtService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<PendingBookingExpirationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IFacilityService, FacilityService>();
        services.AddScoped<IOwnerDashboardService, OwnerDashboardService>();
        services.AddScoped<IActivityHistoryService, ActivityHistoryService>();
        services.AddScoped<IOwnerCourtStatisticsService, OwnerCourtStatisticsService>();
        services.AddScoped<IOwnerCourtService, OwnerCourtService>();
        services.AddScoped<IOwnerFacilityService, OwnerFacilityService>();
        services.AddScoped<IVoucherService, OwnerVoucherService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IOwnerRevenueReportService, OwnerRevenueReportService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
