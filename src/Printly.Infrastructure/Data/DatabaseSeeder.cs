using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Printly.Core.Entities;
using Printly.Core.Enums;

namespace Printly.Infrastructure.Data;

/// <summary>
/// Runs on application startup to ensure the database is in a valid
/// baseline state. Safe to run multiple times — it checks before inserting.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        PrintlyDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        // Apply any pending EF Core migrations automatically on startup.
        // In production you would run migrations as a separate deployment
        // step, but for development this is convenient.
        await context.Database.MigrateAsync();

        // Seed roles — Identity requires roles to exist in the AspNetRoles
        // table before they can be assigned to users.
        var roles = Enum.GetNames<UserRole>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
}