namespace Printly.Core.Entities;

/// <summary>
/// Every entity in Printly inherits from this.
/// Id        — the primary key (Guid = a globally unique ID, like a UUID in JS)
/// CreatedAt — automatically set when a record is first saved
/// UpdatedAt — automatically updated every time the record is saved
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}