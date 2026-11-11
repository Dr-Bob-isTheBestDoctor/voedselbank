namespace BlazorWebTest.Services;

public static class AuthConstants
{
    // Authentication scheme names
    public const string SmartScheme = "LocalOrEntra"; // default: picks one of the schemes below per request
    public const string LocalJwtScheme = "LocalJwt";  // our own JWTs (local MySQL users)
    public const string EntraJwtScheme = "EntraJwt";  // Microsoft Entra ID access tokens sent to /api

    // Name of the HttpOnly cookie that holds the local user's JWT in the browser
    public const string TokenCookieName = "authToken";

    // Roles
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    /// <summary>Only allow redirects inside this site (prevents open-redirect attacks via ?ReturnUrl=).</summary>
    public static string SafeReturnUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
            ? url
            : "/";
}
