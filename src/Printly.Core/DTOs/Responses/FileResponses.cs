using Printly.Core.Enums;

namespace Printly.Core.DTOs.Responses;

public class FileResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public FileStatus Status { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
