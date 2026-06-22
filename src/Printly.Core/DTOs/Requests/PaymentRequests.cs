namespace Printly.Core.DTOs.Requests;

public class InitiatePaymentRequest
{
    public Guid FileId { get; set; }
    public string MoMoPhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// The shape of the JSON body Hubtel sends to our webhook endpoint
/// when a payment succeeds or fails.
/// Field names match what Hubtel actually sends — do not rename them.
/// </summary>
public class HubtelWebhookRequest
{
    public string ResponseCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string ClientReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
