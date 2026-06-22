namespace Printly.Core.DTOs.Responses;

public class QueueItemResponse
{
    public Guid Id { get; set; }
    public Guid FileRecordId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string UploaderName { get; set; } = string.Empty;
    public string? UploaderWhatsApp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool PriceLocked { get; set; }
    public int? PageCount { get; set; }
    public string? PrintingInstructions { get; set; }
    public string? CategoryName { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? PrintingStartedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
}
