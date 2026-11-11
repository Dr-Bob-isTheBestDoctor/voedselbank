using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BlazorWebTest.Services;

/// <summary>
/// Creates the JWTs for local (MySQL) users. Used by both the API (AuthController)
/// and the Blazor login page, so there is one place that knows how tokens look.
/// </summary>
public class JwtTokenService(IConfiguration configuration)
{
    private readonly IConfigurationSection _jwt = configuration.GetSection("Jwt");

    public TimeSpan Lifetime =>
        TimeSpan.FromDays(double.Parse(_jwt["ExpireDays"] ?? "7", CultureInfo.InvariantCulture));

    public string CreateToken(IdentityUser user, IEnumerable<string> roles)
    {
        var key = Encoding.UTF8.GetBytes(_jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(Lifetime),
            Issuer = _jwt["Issuer"],
            Audience = _jwt["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    /// <summary>
    /// Cookie settings for storing the JWT in the browser. HttpOnly means JavaScript can't read it
    /// (safer than localStorage), and the browser sends it automatically with every request.
    /// </summary>
    public CookieOptions CreateCookieOptions(HttpRequest request) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.Add(Lifetime),
        IsEssential = true
    };
}
