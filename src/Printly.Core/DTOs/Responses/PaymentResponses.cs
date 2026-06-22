namespace Printly.Core.DTOs.Responses;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid FileRecordId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? HubtelTransactionId { get; set; }
    public string? MoMoPhoneNumber { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
