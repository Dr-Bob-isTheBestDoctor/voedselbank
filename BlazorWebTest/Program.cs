using System.Security.Claims;
using System.Text;
using BlazorWebTest.Components;
using BlazorWebTest.Data;
using BlazorWebTest.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Blazor
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

// API controllers (AuthController)
builder.Services.AddControllers();

// ---------------------------------------------------------------------------
// 1. Database & Identity (MySQL) - local users and their roles live here
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Detect the MySQL version once (on first use) instead of on every request.
// PublicationOnly = if MySQL is down, try again next time instead of caching the error.
var mySqlVersion = new Lazy<ServerVersion>(
    () => ServerVersion.AutoDetect(connectionString), LazyThreadSafetyMode.PublicationOnly);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, mySqlVersion.Value));

// AddIdentityCore (not AddIdentity): we only need users, roles and password checks.
// AddIdentity would also register Identity's own login cookies, which we don't use.
builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<JwtTokenService>();

// ---------------------------------------------------------------------------
// 2. Authentication: local users (JWT) + admins (Microsoft Entra ID)
//
//    Browser, local user  -> JWT in HttpOnly cookie "authToken"  -> LocalJwt
//    Browser, admin       -> Entra sign-in cookie                -> Cookies (OpenID Connect)
//    API call with header -> "Authorization: Bearer <token>"     -> LocalJwt or EntraJwt (by issuer)
// ---------------------------------------------------------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not found");
var localIssuer = jwtSection["Issuer"];

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = AuthConstants.SmartScheme;
    options.DefaultChallengeScheme = AuthConstants.SmartScheme;
});

// Decides per request which of the schemes below should handle it
authBuilder.AddPolicyScheme(AuthConstants.SmartScheme, "Local JWT or Microsoft Entra ID", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        string? authHeader = context.Request.Headers.Authorization;
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var handler = new JsonWebTokenHandler();
            var isLocalToken = handler.CanReadToken(token) && handler.ReadJsonWebToken(token).Issuer == localIssuer;
            return isLocalToken ? AuthConstants.LocalJwtScheme : AuthConstants.EntraJwtScheme;
        }

        if (context.Request.Cookies.ContainsKey(AuthConstants.TokenCookieName) || IsApiRequest(context.Request))
        {
            return AuthConstants.LocalJwtScheme;
        }

        // Entra sign-in cookie (also handles "not logged in": redirects to /Login)
        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
});

// Local users: our own JWT, from the Authorization header (API) or the authToken cookie (browser)
authBuilder.AddJwtBearer(AuthConstants.LocalJwtScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = localIssuer,
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (!context.Request.Headers.ContainsKey("Authorization") &&
                context.Request.Cookies.TryGetValue(AuthConstants.TokenCookieName, out var cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        },
        // Not logged in / token expired: pages redirect to /Login, the API returns 401
        OnChallenge = context =>
        {
            if (!IsApiRequest(context.Request))
            {
                context.HandleResponse();
                context.Response.Cookies.Delete(AuthConstants.TokenCookieName);
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            return Task.CompletedTask;
        },
        // Logged in but wrong role (e.g. a "User" opening Voorraad): pages redirect, the API returns 403
        OnForbidden = context =>
        {
            if (!IsApiRequest(context.Request))
            {
                context.Response.Redirect("/access-denied");
            }
            return Task.CompletedTask;
        }
    };
});

// Admins in the browser: Microsoft Entra ID sign-in (OpenID Connect + "Cookies" cookie)
authBuilder.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Admins calling /api with an Entra access token
authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"),
    jwtBearerScheme: AuthConstants.EntraJwtScheme);

// Anyone signing in via Entra ID gets the Admin role (same rule as before)
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var previous = options.Events.OnTokenValidated;
    options.Events.OnTokenValidated = async context =>
    {
        if (previous is not null) await previous(context);
        AddAdminRole(context.Principal);
    };
});
builder.Services.Configure<JwtBearerOptions>(AuthConstants.EntraJwtScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    var previous = options.Events.OnTokenValidated;
    options.Events.OnTokenValidated = async context =>
    {
        if (previous is not null) await previous(context);
        AddAdminRole(context.Principal);
    };
});

// Send not-logged-in visitors to our own login page instead of straight to Microsoft
builder.Services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/access-denied";
});

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
var app = builder.Build();

// Apply migrations + create the Admin/User roles (and a test user in Development)
await IdentitySeeder.SeedAsync(app.Services, app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// The API should answer 401/403 as-is, not with the HTML "Not Found" page
app.Use(async (context, next) =>
{
    if (IsApiRequest(context.Request) && context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
    {
        statusCodePages.Enabled = false;
    }
    await next(context);
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

// Admin login: starts the Microsoft Entra ID sign-in
app.MapGet("/account/login-entra", (HttpContext context, string? returnUrl) =>
{
    context.Response.Cookies.Delete(AuthConstants.TokenCookieName); // end any local session
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = AuthConstants.SafeReturnUrl(returnUrl) },
        [OpenIdConnectDefaults.AuthenticationScheme]);
});

// Logout for both kinds of users
app.MapPost("/account/logout", async (HttpContext context) =>
{
    context.Response.Cookies.Delete(AuthConstants.TokenCookieName);

    var entraSession = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (entraSession.Succeeded)
    {
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }

    return Results.LocalRedirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
static bool IsApiRequest(HttpRequest request) => request.Path.StartsWithSegments("/api");

static void AddAdminRole(ClaimsPrincipal? principal)
{
    if (principal?.Identity is ClaimsIdentity identity && !identity.HasClaim(identity.RoleClaimType, AuthConstants.AdminRole))
    {
        identity.AddClaim(new Claim(identity.RoleClaimType, AuthConstants.AdminRole));
    }
}
