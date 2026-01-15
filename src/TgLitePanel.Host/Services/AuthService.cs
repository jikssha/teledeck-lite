using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TgLitePanel.Core.Abstractions.Stores;

namespace TgLitePanel.Host.Services;

public sealed class AuthService
{
    private readonly IUserStore _userStore;
    private readonly PasswordHasher _hasher;

    public AuthService(IUserStore userStore, PasswordHasher hasher)
    {
        _userStore = userStore;
        _hasher = hasher;
    }

    public async Task<UserRecord?> ValidateAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        var user = await _userStore.FindByUsernameAsync(username.Trim(), cancellationToken);
        if (user is null)
            return null;

        return _hasher.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task SignInAsync(HttpContext httpContext, UserRecord user, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}

