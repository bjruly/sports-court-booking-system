using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Security;
using SportsCourtBooking.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký DbContext kết nối PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký các Application Services (Dependency Injection)
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Đăng nhập bằng cookie xác thực (được mã hóa/ký bởi ASP.NET Core, người dùng không tự sửa được)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.Cookie.Name = "SanNha.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Tự tạo database và chạy migration khi khởi động.
// PostgreSQL trong Docker có thể khởi động chậm hơn web nên thử lại tối đa 30 lần (mỗi lần cách 3 giây).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    const int maxAttempts = 30;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            // Có file migration thì chạy migration; chưa có (chưa chạy "dotnet ef migrations add") thì
            // tạo bảng trực tiếp từ model, kèm dữ liệu mẫu (HasData).
            if (db.Database.GetMigrations().Any())
                db.Database.Migrate();
            else
                db.Database.EnsureCreated();

            // Dữ liệu mẫu có Id cố định nên cần đồng bộ lại bộ đếm Id, nếu không bản ghi mới sẽ bị trùng Id.
            db.SyncIdentitySequences();
            logger.LogInformation("Database đã sẵn sàng.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning("Database chưa sẵn sàng (lần {Attempt}/{Max}): {Message}. Thử lại sau 3 giây...",
                attempt, maxAttempts, ex.Message);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
