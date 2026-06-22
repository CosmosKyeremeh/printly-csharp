namespace Printly.Core.DTOs.Requests;

public class FileUploadRequest
{
    public string? Instructions { get; set; }
    public Guid? CategoryId { get; set; }
}
