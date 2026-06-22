using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Printly.Core.Entities;
using Printly.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ── 1. DATABASE ────────────────────────────────────────────────────────────
// Register PrintlyDbContext with the DI container.
// "DI container" = Dependency Injection container — a system that
// automatically creates and provides objects (services) wherever they
// are needed, so you never call `new PrintlyDbContext()` manually.
//
// UseNpgsql tells EF Core to use PostgreSQL as the database engine.
// The connection string comes from appsettings.Development.json.
builder.Services.AddDbContext<PrintlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. IDENTITY ────────────────────────────────────────────────────────────
// AddIdentity registers all of Identity's services:
//   - UserManager<AppUser>  — create/find/update/delete users
//   - RoleManager           — create/assign/check roles
//   - SignInManager         — handle login/logout sessions
//
// AddEntityFrameworkStores tells Identity to store its data
// (users, roles, claims) in our PrintlyDbContext / PostgreSQL database.
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    // Password rules — adjust these to your preference.
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    // Email must be unique — no two users share an email address.
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<PrintlyDbContext>()
.AddDefaultTokenProviders(); // Needed for password reset tokens

// ── 3. MVC + RAZOR PAGES ──────────────────────────────────────────────────
// AddControllersWithViews enables:
//   - API Controllers (our REST endpoints)
//   - MVC Controllers with Razor Views
builder.Services.AddControllersWithViews();

// AddRazorPages enables Razor Pages (the student/admin dashboards)
builder.Services.AddRazorPages();

// ── 4. SWAGGER ────────────────────────────────────────────────────────────
// Swagger auto-generates an interactive API documentation page
// at /swagger where you can test every endpoint in the browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── BUILD ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── 5. SEED THE DATABASE ───────────────────────────────────────────────────
// Run the seeder on every startup (it's safe — checks before inserting).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PrintlyDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
}

// ── 6. MIDDLEWARE PIPELINE ────────────────────────────────────────────────
// Middleware = a chain of functions that every HTTP request passes through
// before reaching your controller. Order matters — they run top to bottom.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Serves the interactive UI at /swagger
}

app.UseHttpsRedirection(); // Redirect HTTP → HTTPS
app.UseStaticFiles();      // Serve files from wwwroot/ (CSS, JS, images)
app.UseRouting();          // Match incoming URLs to controllers/pages

app.UseAuthentication();   // Read the JWT/cookie and identify the user
app.UseAuthorization();    // Check if the identified user is allowed to proceed

// Map Razor Pages (student/admin dashboards)
app.MapRazorPages();

// Map MVC controllers (API endpoints)
app.MapControllers();

app.Run();