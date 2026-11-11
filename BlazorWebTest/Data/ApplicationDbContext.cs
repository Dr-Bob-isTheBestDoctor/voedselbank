using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorWebTest.Data;

// Holds the ASP.NET Core Identity tables (users, roles, user-roles, ...) in MySQL.
// Add your own DbSet<T> properties here later (e.g. for Voorraad).
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
}
