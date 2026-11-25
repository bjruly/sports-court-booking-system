using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure;
using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext và các service nghiệp vụ, dùng chung với ứng dụng WinForms
builder.Services.AddSportsCourtServices(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddHostedService<PendingBookingExpirationWorker>();

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
            await db.BaselineExistingDatabaseAsync();
            await db.UpgradeLegacyFacilitySchemaAsync();
            db.Database.Migrate();

            if (!await db.Users.AnyAsync(user => user.Email == SeedCredentials.OwnerEmail))
            {
                db.Users.Add(new User
                {
                    FullName = "Chủ Sân Demo",
                    Email = SeedCredentials.OwnerEmail,
                    PhoneNumber = "0901111111",
                    PasswordHash = SeedCredentials.OwnerPasswordHash,
                    Role = UserRole.CourtOwner,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }

            await db.EnsureCourtOwnerSchemaAsync();
            await db.EnsureOwnerFacilitySchemaAsync();
            await db.EnsureCourtPricingSchemaAsync();
            await db.EnsureCourtDistrictSchemaAsync();
            await db.EnsureVoucherSchemaAsync();
            await db.EnsureBookingSchemaAsync();
            await db.EnsureReviewSchemaAsync();

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
