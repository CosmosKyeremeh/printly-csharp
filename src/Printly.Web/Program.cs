using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Printly.Core.Entities;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;
using Printly.Infrastructure.Services;
using Printly.Infrastructure.Storage;
using Printly.Web.Hubs;
using Printly.Web.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── 1. DATABASE ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<PrintlyDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. IDENTITY ────────────────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<PrintlyDbContext>()
.AddDefaultTokenProviders();

// ── 3. AUTHENTICATION — Cookie + JWT ──────────────────────────────────────
// Razor Pages use cookies (browser-friendly).
// API Controllers use JWT Bearer tokens (API-friendly).
// We support both simultaneously.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key not configured.");

// AddIdentity() above already registered its own cookie scheme
// (IdentityConstants.ApplicationScheme) and pinned it as the default
// authenticate scheme. We override that here so the policy scheme below
// actually gets consulted — otherwise [Authorize] on API controllers
// would authenticate against the Identity cookie only and JWT bearer
// tokens would never be checked.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.AccessDeniedPath = "/auth/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CookieOrJwt";
    options.DefaultAuthenticateScheme = "CookieOrJwt";
    options.DefaultChallengeScheme = "CookieOrJwt";
})
.AddJwtBearer("JWT", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // SignalR sends JWT as query param for WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
// Policy scheme picks the right auth scheme based on the request
.AddPolicyScheme("CookieOrJwt", "CookieOrJwt", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // API routes and hubs use JWT
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/hubs"))
            return "JWT";

        // Everything else (Razor Pages) uses the Identity cookie
        return IdentityConstants.ApplicationScheme;
    };
});

// ── 4. AUTHORIZATION ───────────────────────────────────────────────────────
builder.Services.AddAuthorization();

// ── 5. SIGNALR ────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── 6. APPLICATION SERVICES ───────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<INotificationPusher, SignalRNotificationPusher>();

// ── 7. MVC + RAZOR + SWAGGER ──────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── 8. SEED DATABASE ──────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PrintlyDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
}

// ── 9. MIDDLEWARE ─────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── 10. ENDPOINTS ─────────────────────────────────────────────────────────
app.MapControllers();
app.MapRazorPages();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
