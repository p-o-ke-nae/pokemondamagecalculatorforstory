using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Extensions;

namespace PokemonDamageCalculatorForStory.Controllers.Admin;

public sealed class AdminSessionController(
    IGoogleAccessTokenValidationService tokenValidationService,
    IUserAuthorizationInfoRepository userAuthorizationRepository) : Controller
{
    [HttpGet("admin/login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl ?? "/admin/masters/rule-sets";
        return View();
    }

    [HttpPost("admin/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] string accessToken, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        var validation = await tokenValidationService.ValidateAsync(accessToken, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddModelError(string.Empty, validation.FailureReason ?? "Google access token validation failed.");
            ViewData["ReturnUrl"] = returnUrl ?? "/admin/masters/rule-sets";
            return View();
        }

        var userAuthorization = await userAuthorizationRepository.FindByGoogleUserIdAsync(validation.GoogleUserId!, cancellationToken);
        var claims = new List<Claim>
        {
            new(GoogleClaimTypes.GoogleUserId, validation.GoogleUserId!),
            new(ClaimTypes.NameIdentifier, validation.GoogleUserId!),
            new(ClaimTypes.Email, validation.Email!),
            new(ClaimTypes.Name, validation.Name!),
            new(ClaimTypes.Role, userAuthorization?.Role ?? AppRoles.Member)
        };

        foreach (var permission in userAuthorization?.Permissions ?? [])
        {
            claims.Add(new Claim("permission", permission));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AdminCookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(AdminCookieAuthenticationDefaults.AuthenticationScheme, principal);

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin/masters/rule-sets" : returnUrl);
    }

    [HttpPost("admin/logout")]
    [Authorize(AuthenticationSchemes = AdminCookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AdminCookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
