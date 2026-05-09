using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SkiRental.Domain.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkiRental.App.Services;

/// <summary>
/// Generates and validates JWT tokens.
/// Both <see cref="UserRole"/> (legacy) and <see cref="AppRole"/> overloads are provided
/// so existing components don't need to change while new ones use AppRole directly.
/// </summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _handler = new JwtSecurityTokenHandler();
    }

    // ── Generate ─────────────────────────────────────────────────────────────

    /// <summary>Generate token from legacy UserRole (backward compat).</summary>
    public string GenerateToken(string userId, string username, UserRole role)
        => BuildToken(userId, username, role.ToString());

    /// <summary>Generate token from AppRole (new code should use this overload).</summary>
    public string GenerateToken(string userId, string username, AppRole role)
        => BuildToken(userId, username, role.ToString());

    private string BuildToken(string userId, string username, string roleString)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, roleString),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(claims),
            Expires            = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            SigningCredentials = creds,
            Issuer             = _options.Issuer,
            Audience           = _options.Audience
        };

        return _handler.WriteToken(_handler.CreateToken(descriptor));
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = _options.Issuer,
                ValidAudience            = _options.Audience,
                IssuerSigningKey         = key,
                ClockSkew                = TimeSpan.FromMinutes(_options.ClockSkewMinutes)
            };

            return _handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    // ── Claim helpers ─────────────────────────────────────────────────────────

    public string? GetUserIdFromToken(string token)
        => ValidateToken(token)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? GetUsernameFromToken(string token)
        => ValidateToken(token)?.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>Returns the AppRole claim from the token, or null if invalid.</summary>
    public AppRole? GetAppRoleFromToken(string token)
    {
        var claim = ValidateToken(token)?.FindFirst(ClaimTypes.Role)?.Value;
        return claim is not null && Enum.TryParse<AppRole>(claim, out var r) ? r : null;
    }

    /// <summary>Legacy helper — returns UserRole for components not yet migrated to AppRole.</summary>
    public UserRole? GetRoleFromToken(string token)
    {
        var appRole = GetAppRoleFromToken(token);
        return appRole switch
        {
            AppRole.Owner or AppRole.Worker => UserRole.Worker,
            AppRole.Customer => UserRole.Customer,
            _ => null
        };
    }
}
