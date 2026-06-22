namespace Printly.Core.DTOs.Requests;

public class SetPriceRequest
{
    public decimal Price { get; set; }
}

public class UpdateFileStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class BulkStatusUpdateRequest
{
    public List<Guid> FileIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
