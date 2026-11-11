using BlazorWebTest.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorWebTest.Data;

public static class IdentitySeeder
{
    // Development-only test account (replaces the old hardcoded "user" / "password123" check).
    private const string DevUserName = "user";
    private const string DevPassword = "Password123!";

    /// <summary>
    /// Applies pending EF migrations, creates the Admin/User roles and (in Development) a test user.
    /// Never crashes the app: if MySQL isn't reachable, it logs what to do and carries on.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, bool isDevelopment)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { AuthConstants.AdminRole, AuthConstants.UserRole })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            if (isDevelopment)
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                if (await userManager.FindByNameAsync(DevUserName) is null)
                {
                    var user = new IdentityUser { UserName = DevUserName };
                    var result = await userManager.CreateAsync(user, DevPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, AuthConstants.UserRole);
                        logger.LogInformation("Created development user '{User}' with password '{Password}'.", DevUserName, DevPassword);
                    }
                    else
                    {
                        logger.LogWarning("Could not create development user: {Errors}",
                            string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Could not migrate/seed the MySQL database. Check that MySQL is running, that the password in " +
                "appsettings.json (ConnectionStrings:DefaultConnection) is correct, and that you have created a " +
                "migration with: dotnet ef migrations add InitialIdentity");
        }
    }
}
