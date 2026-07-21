using Microsoft.Extensions.Hosting;
using Printly.Core.Interfaces;

namespace Printly.Infrastructure.Storage;

/// <summary>
/// Stores files on the local disk inside a folder called "uploads"
/// at the root of the application. Used in development only.
/// IHostEnvironment gives us the path to the app's content root folder.
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly string _basePath;

    public LocalStorageService(IHostEnvironment env)
    {
        _basePath = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(
        Stream fileStream, string fileName, string contentType)
    {
        var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_basePath, uniqueName);

        await using var fileOut = File.Create(fullPath);
        await fileStream.CopyToAsync(fileOut);

        return uniqueName;
    }

    public Task<Stream> DownloadAsync(string storagePath)
    {
        var fullPath = Path.Combine(_basePath, storagePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found.", storagePath);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
