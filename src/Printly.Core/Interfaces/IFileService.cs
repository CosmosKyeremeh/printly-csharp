using Microsoft.AspNetCore.Http;
using Printly.Core.DTOs.Responses;

namespace Printly.Core.Interfaces;

public interface IFileService
{
    Task<List<FileResponse>> UploadFilesAsync(
        List<IFormFile> files,
        string? instructions,
        Guid? categoryId,
        Guid userId,
        Guid orgId);

    Task<List<FileResponse>> GetUserFilesAsync(Guid userId, Guid orgId);
    Task<List<FileResponse>> GetOrgFilesAsync(Guid orgId);

    Task<FileResponse> GetFileByIdAsync(Guid fileId, Guid userId, Guid orgId);

    Task<(Stream stream, string contentType, string fileName)> DownloadFileAsync(
        Guid fileId,
        Guid userId,
        Guid orgId);

    Task DeleteFileAsync(Guid fileId, Guid userId, Guid orgId);
    Task<FileResponse> ConvertFileAsync(Guid fileId, Guid userId, Guid orgId);
}