namespace Printly.Core.Interfaces;

/// <summary>
/// Abstracts file storage so the rest of the app doesn't care whether
/// files are stored on local disk (dev) or Azure Blob Storage (prod).
/// Swapping storage providers = swap the implementation, nothing else changes.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Upload a file stream and return the storage path/blob name.
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Return a readable stream for downloading a stored file.
    /// </summary>
    Task<Stream> DownloadAsync(string storagePath);

    /// <summary>
    /// Permanently delete a file from storage.
    /// Called when a soft-deleted file is permanently purged.
    /// </summary>
    Task DeleteAsync(string storagePath);
}
