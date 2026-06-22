using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Printly.Core.Entities;
using Printly.Core.Enums;

namespace Printly.Infrastructure.Data;

/// <summary>
/// The central database context for Printly.
///
/// Inherits from IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
/// which means ASP.NET Core Identity's tables (Users, Roles, UserRoles,
/// UserClaims, etc.) are automatically included alongside our own tables.
///
/// The three generic parameters tell Identity:
///   AppUser          — use OUR custom user class, not the default IdentityUser
///   IdentityRole<Guid> — use Guid as the type for role IDs
///   Guid             — use Guid as the type for all Identity primary keys
/// </summary>
public class PrintlyDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    // ── Constructor ────────────────────────────────────────────────────────
    // DbContextOptions carries the connection string and provider info
    // (i.e. "use PostgreSQL at this connection string").
    // It is passed in from Program.cs when the app starts.
    public PrintlyDbContext(DbContextOptions<PrintlyDbContext> options)
        : base(options)
    {
    }

    // ── DbSets ────────────────────────────────────────────────────────────
    // Each DbSet<T> maps to one table in the database.
    // DbSet<Organization> → "Organizations" table
    // You query them like: _context.Organizations.Where(o => o.Name == "CS3")
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FileRecord> FileRecords => Set<FileRecord>();
    public DbSet<PrintQueueItem> PrintQueueItems => Set<PrintQueueItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AdminResource> AdminResources => Set<AdminResource>();
    public DbSet<FileComment> FileComments => Set<FileComment>();

    // ── Model Configuration ────────────────────────────────────────────────
    // OnModelCreating is called once when the app starts.
    // This is where we configure relationships, constraints,
    // global query filters, and column types that EF Core
    // cannot figure out automatically from the C# classes alone.
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IMPORTANT: Always call the base method first when using Identity.
        // It sets up all the Identity tables (AspNetUsers, AspNetRoles, etc.)
        base.OnModelCreating(builder);

        // ── 1. GLOBAL QUERY FILTERS ────────────────────────────────────────
        // This is the multi-tenancy enforcement layer.
        //
        // A Global Query Filter is a WHERE clause that EF Core automatically
        // appends to EVERY query on a DbSet — you never have to remember to
        // add .Where(x => !x.DeletedAt.HasValue) yourself.
        //
        // Soft delete filter: any entity with DeletedAt set is invisible
        // to all normal queries. It still exists in the DB but won't appear.
        builder.Entity<FileRecord>()
            .HasQueryFilter(f => f.DeletedAt == null);

        builder.Entity<PrintQueueItem>()
            .HasQueryFilter(q => q.FileRecord.DeletedAt == null);

        // ── 2. ORGANIZATION CONFIGURATION ─────────────────────────────────
        builder.Entity<Organization>(entity =>
        {
            // JoinCode must be unique across all organizations.
            entity.HasIndex(o => o.JoinCode).IsUnique();

            // One Organization has many Members (AppUsers).
            // If an org is deleted, what happens to its users?
            // We restrict deletion — you cannot delete an org that still
            // has members. This prevents accidental data loss.
            entity.HasMany(o => o.Members)
                  .WithOne(u => u.Organization)
                  .HasForeignKey(u => u.OrgId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(o => o.Categories)
                  .WithOne(c => c.Organization)
                  .HasForeignKey(c => c.OrgId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.Notifications)
                  .WithOne(n => n.Organization)
                  .HasForeignKey(n => n.OrgId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.Resources)
                  .WithOne(r => r.Organization)
                  .HasForeignKey(r => r.OrgId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── 3. APPUSER CONFIGURATION ───────────────────────────────────────
        builder.Entity<AppUser>(entity =>
        {
            // Store the Role enum as its string name ("Student", "Admin")
            // rather than an integer (0, 1, 2).
            // Strings are far easier to read in the database and safer
            // when you add new enum values later.
            entity.Property(u => u.Role)
                  .HasConversion<string>();

            entity.HasMany(u => u.Files)
                  .WithOne(f => f.User)
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.Payments)
                  .WithOne(p => p.User)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── 4. FILERECORD CONFIGURATION ────────────────────────────────────
        builder.Entity<FileRecord>(entity =>
        {
            // Store enums as strings for readability
            entity.Property(f => f.Status)
                  .HasConversion<string>();

            entity.Property(f => f.PaymentStatus)
                  .HasConversion<string>();

            // Price is money — use decimal(18,2) in Postgres.
            // This means up to 18 digits total, 2 after the decimal point.
            // Never use float or double for money — they have rounding errors.
            entity.Property(f => f.Price)
                  .HasColumnType("decimal(18,2)");

            // One FileRecord has exactly one PrintQueueItem.
            // This is a one-to-one relationship.
            entity.HasOne(f => f.QueueItem)
                  .WithOne(q => q.FileRecord)
                  .HasForeignKey<PrintQueueItem>(q => q.FileRecordId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(f => f.Comments)
                  .WithOne(c => c.FileRecord)
                  .HasForeignKey(c => c.FileRecordId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(f => f.Payments)
                  .WithOne(p => p.FileRecord)
                  .HasForeignKey(p => p.FileRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── 5. PAYMENT CONFIGURATION ───────────────────────────────────────
        builder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Method)
                  .HasConversion<string>();

            entity.Property(p => p.Status)
                  .HasConversion<string>();

            entity.Property(p => p.Amount)
                  .HasColumnType("decimal(18,2)");
        });

        // ── 6. NOTIFICATION CONFIGURATION ─────────────────────────────────
        builder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Type)
                  .HasConversion<string>();

            // ReadByUserIds is stored as a native PostgreSQL uuid[] array.
            // Npgsql (the Postgres driver) handles the serialization
            // between List<Guid> in C# and uuid[] in Postgres automatically.
            entity.Property(n => n.ReadByUserIds)
                  .HasColumnType("uuid[]");
        });
    }

    // ── SaveChangesAsync Override ──────────────────────────────────────────
    // This runs automatically every time we save to the database.
    // It handles two things:
    //   1. Auto-sets UpdatedAt on every modified entity
    //   2. Converts Delete operations to soft deletes on FileRecord
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            // Auto-timestamp: any entity with UpdatedAt gets it set on save
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Added)
                    baseEntity.CreatedAt = now;

                if (entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified)
                    baseEntity.UpdatedAt = now;
            }

            // Soft delete interception:
            // When something tries to delete a FileRecord,
            // we intercept it and set DeletedAt instead.
            if (entry.Entity is FileRecord fileRecord &&
                entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                fileRecord.DeletedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}