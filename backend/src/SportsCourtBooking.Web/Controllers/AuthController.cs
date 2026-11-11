using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;

namespace SportsCourtBooking.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success || result.User == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Login", "Home");
        }

        // Lưu thông tin cơ bản vào TempData/Cookies để hiển thị tên người dùng
        TempData["SuccessMessage"] = $"Xin chào {result.User.FullName} ({result.User.Role})!";
        Response.Cookies.Append("CurrentUserId", result.User.Id.ToString());
        Response.Cookies.Append("CurrentUserName", result.User.FullName);
        Response.Cookies.Append("CurrentUserRole", result.User.Role.ToString());

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Register", "Home");
        }

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login", "Home");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("CurrentUserId");
        Response.Cookies.Delete("CurrentUserName");
        Response.Cookies.Delete("CurrentUserRole");
        TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }
}