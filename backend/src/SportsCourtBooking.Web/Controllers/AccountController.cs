using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Web.Extensions;
using System.Security.Claims;

namespace SportsCourtBooking.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Challenge();
        }

        var profile = await _accountService.GetProfileAsync(
            userId.Value
        );

        if (profile is null)
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            TempData["ErrorMessage"] =
                "Tài khoản không tồn tại hoặc đã bị khóa. Vui lòng đăng nhập lại.";

            return RedirectToAction("Login", "Home");
        }

        ViewData["Email"] = profile.Email;

        var model = new UpdateProfileRequest
        {
            FullName = profile.FullName,
            PhoneNumber = profile.PhoneNumber
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
public async Task<IActionResult> Profile(UpdateProfileRequest request)
{
    var userId = User.GetUserId();

    if (userId is null)
    {
        return Challenge();
    }

    var profile = await _accountService.GetProfileAsync(
        userId.Value
    );

    if (profile is null)
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        TempData["ErrorMessage"] =
            "Tài khoản không tồn tại hoặc đã bị khóa. Vui lòng đăng nhập lại.";

        return RedirectToAction("Login", "Home");
    }

    ViewData["Email"] = profile.Email;

    if (!ModelState.IsValid)
    {
        return View(request);
    }

    var result = await _accountService.UpdateProfileAsync(
        userId.Value,
        request
    );

    if (!result.Success)
    {
        ModelState.AddModelError(string.Empty, result.Message);

        return View(request);
    }

    var identity = new ClaimsIdentity(
        User.Claims,
        CookieAuthenticationDefaults.AuthenticationScheme
    );

    var oldNameClaim = identity.FindFirst(ClaimTypes.Name);

    if (oldNameClaim != null)
    {
        identity.RemoveClaim(oldNameClaim);
    }

    identity.AddClaim(
        new Claim(ClaimTypes.Name, request.FullName.Trim())
    );

    var authentication = await HttpContext.AuthenticateAsync(
        CookieAuthenticationDefaults.AuthenticationScheme
    );

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        authentication.Properties
    );

    TempData["SuccessMessage"] = result.Message;

    return RedirectToAction(nameof(Profile));
}
[HttpGet]
public async Task<IActionResult> ChangePassword()
{
    var userId = User.GetUserId();

    if (userId is null)
    {
        return Challenge();
    }

    var profile = await _accountService.GetProfileAsync(
        userId.Value
    );

    if (profile is null)
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        TempData["ErrorMessage"] =
            "Tài khoản không tồn tại hoặc đã bị khóa. Vui lòng đăng nhập lại.";

        return RedirectToAction("Login", "Home");
    }

    return View(new ChangePasswordRequest());
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ChangePassword(
    ChangePasswordRequest request)
{
    var userId = User.GetUserId();

    if (userId is null)
    {
        return Challenge();
    }

    if (!ModelState.IsValid)
    {
        return View(new ChangePasswordRequest());
    }

    var result = await _accountService.ChangePasswordAsync(
        userId.Value,
        request
    );

    if (!result.Success)
    {
        ModelState.AddModelError(string.Empty, result.Message);

        return View(new ChangePasswordRequest());
    }

    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme
    );

    TempData["SuccessMessage"] = result.Message;

    return RedirectToAction("Login", "Home");
}
}