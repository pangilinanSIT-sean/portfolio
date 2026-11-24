using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using portfolio.Models;

namespace portfolio.Controllers;

// Signing in and out. The editor password is kept in User Secrets (Editor:Password), never in the repository.
public class AccountController : Controller
{
    private readonly IConfiguration _config;

    public AccountController(IConfiguration config)
    {
        _config = config;
    }

    // GET /Account/Login
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginForm());
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var expected = _config["Editor:Password"];
        if (string.IsNullOrEmpty(expected))
        {
            ModelState.AddModelError("", "No editor password is set yet. Add Editor:Password in Manage User Secrets.");
            return View(form);
        }

        // Compare in constant time, so the time taken does not hint at how much of the password was right.
        bool same = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(form.Password ?? ""), Encoding.UTF8.GetBytes(expected));

        if (!same)
        {
            ModelState.AddModelError("Password", "That is not the editor password.");
            return View(form);
        }

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "editor") },
                                          CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // Go back to where the visitor was heading, but only inside this site.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Courses");
    }

    // POST /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}