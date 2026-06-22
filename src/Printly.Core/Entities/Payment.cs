using Printly.Core.Enums;

namespace Printly.Core.Entities;

/// <summary>
/// Records a payment transaction for a file.
/// For MoMo: HubtelTransactionId links to the Hubtel payment record.
/// For Cash: marked manually by an admin, no external transaction ID.
/// </summary>
public class Payment : BaseEntity
{
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Hubtel's transaction reference — null for cash payments.
    public string? HubtelTransactionId { get; set; }

    // The phone number the MoMo charge was sent to.
    public string? MoMoPhoneNumber { get; set; }

    // When Hubtel confirmed the payment via webhook.
    public DateTime? PaidAt { get; set; }

    // Foreign keys
    public Guid FileRecordId { get; set; }
    public FileRecord FileRecord { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid OrgId { get; set; }
    public Organization Organization { get; set; } = null!;
}