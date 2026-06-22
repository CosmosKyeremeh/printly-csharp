namespace Printly.Core.Entities;

/// <summary>
/// The root of the multi-tenant system.
/// One Organization = one class or cohort.
/// Every piece of data in Printly belongs to exactly one Organization.
/// </summary>
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // The unique code students type at signup to join this org.
    // Generated automatically when the org is created.
    public string JoinCode { get; set; } = string.Empty;

    // The user who created this organization becomes its Superadmin.
    public Guid CreatedByUserId { get; set; }

    // Navigation properties — EF Core uses these to do JOINs automatically.
    // They are not columns themselves; they are pointers to related records.
    public ICollection<AppUser> Members { get; set; } = new List<AppUser>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AdminResource> Resources { get; set; } = new List<AdminResource>();
}