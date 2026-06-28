using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Printly.Core.Entities;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;
using Printly.Infrastructure.Services;
using Printly.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// -- 1. DATABASE ------------------------------------------------------------
// Register PrintlyDbContext with the DI container.
// UseNpgsql tells EF Core to use PostgreSQL as the database engine.
builder.Services.AddDbContext<PrintlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// -- 2. IDENTITY ------------------------------------------------------------
// AddIdentity registers all of Identity's core user management services.
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

// -- 3. APPLICATION SERVICES (DEPENDENCY INJECTION) -------------------------
// This maps our Core Interfaces to their Concrete Infrastructure implementations.
// AddScoped creates ONE instance per HTTP request lifecycle.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserService, UserService>();

// File Storage Layer registration
builder.Services.AddScoped<IStorageService, LocalStorageService>();

// CurrentUserService reads custom user claims from the current HTTP session.
// HttpContextAccessor must be explicitly added to expose those session vectors.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();

// -- 4. MVC + RAZOR PAGES --------------------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// -- 5. SWAGGER ------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -- BUILD ------------------------------------------------------------------
var app = builder.Build();

// -- 6. SEED THE DATABASE ---------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PrintlyDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
}

// -- 7. MIDDLEWARE PIPELINE ------------------------------------------------
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

app.MapRazorPages();
app.MapControllers();

app.Run();
