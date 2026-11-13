using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = JoinModelErrors();
            return RedirectToAction("Login", "Home", new { returnUrl });
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Success || result.User == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Login", "Home", new { returnUrl });
        }

        var user = result.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        TempData["SuccessMessage"] = $"Xin chào {user.FullName}!";

        // Chỉ cho phép quay lại địa chỉ nội bộ của website (chống open redirect)
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = JoinModelErrors();
            return RedirectToAction("Register", "Home", new { returnUrl });
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Register", "Home", new { returnUrl });
        }

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login", "Home", new { returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }

    private string JoinModelErrors()
    {
        var messages = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToArray();

        return messages.Length > 0 ? string.Join(" ", messages) : "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
    }
}
