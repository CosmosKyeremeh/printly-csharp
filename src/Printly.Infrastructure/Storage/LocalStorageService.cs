using Microsoft.Extensions.Hosting;
using Printly.Core.Interfaces;

namespace Printly.Infrastructure.Storage;

/// <summary>
/// STUB implementation of IStorageService.
/// Files are not physically stored anywhere — this is intentional for
/// the academic project demo. The upload flow, queue, and status tracking
/// all work normally. Only the actual file bytes are discarded.
///
/// To enable real storage later, swap this registration in Program.cs
/// for either LocalStorageService (disk) or AzureBlobStorageService (cloud).
/// </summary>
public class LocalStorageService : IStorageService
{
    public async Task<string> UploadAsync(
        Stream fileStream, string fileName, string contentType)
    {
        // Drain the stream so the HTTP request completes normally.
        // Without this, the client would hang waiting for the server
        // to finish reading the upload.
        await fileStream.CopyToAsync(Stream.Null);

        // Return a fake storage path so the FileRecord has something
        // to store in the StoragePath column.
        return $"stub/{Guid.NewGuid()}_{fileName}";
    }

    public Task<Stream> DownloadAsync(string storagePath)
    {
        // Return an empty stream — download will produce a 0-byte file.
        // For demo purposes this is fine.
        Stream empty = new MemoryStream();
        return Task.FromResult(empty);
    }

    public Task DeleteAsync(string storagePath)
    {
        // Nothing to delete — just return.
        return Task.CompletedTask;
    }
}
