namespace Printly.Core.DTOs.Responses;

public class FileResponse
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int? PageCount { get; set; }
    public decimal Price { get; set; }
    public bool PriceLocked { get; set; }
    public string? PrintingInstructions { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string UploaderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? QueuePosition { get; set; }
}