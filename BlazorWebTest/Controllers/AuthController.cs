using System.Security.Claims;
using BlazorWebTest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebTest.Controllers;

/// <summary>
/// JSON API for clients other than the Blazor pages (e.g. a mobile app, Postman, the .http file).
/// The Blazor login page (Local-Login.razor) does the same thing directly, without an HTTP round trip.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    JwtTokenService tokenService) : ControllerBase
{
    // POST api/auth/login  { "username": "...", "password": "..." }  ->  { token, username, roles }
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await userManager.FindByNameAsync(model.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        // Get user roles from database (e.g., "User")
        var roles = await userManager.GetRolesAsync(user);

        return Ok(new AuthResponseDto
        {
            Token = tokenService.CreateToken(user, roles),
            Username = user.UserName ?? "",
            Roles = roles.ToList()
        });
    }

    // GET api/auth/me  (send "Authorization: Bearer <token>")  ->  who am I? Works for local and Entra tokens.
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var identity = User.Identity as ClaimsIdentity;
        var roles = identity is null
            ? Enumerable.Empty<string>()
            : identity.FindAll(identity.RoleClaimType).Select(c => c.Value);

        return Ok(new { name = User.Identity?.Name, authenticationType = User.Identity?.AuthenticationType, roles });
    }
}

// DTO Models
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
