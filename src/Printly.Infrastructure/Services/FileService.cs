using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Entities;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;

namespace Printly.Infrastructure.Services;

/// <summary>
/// Handles all file upload, retrieval, and management logic.
/// Maps incoming IFormFile abstractions cleanly to stream vectors to maintain contract fidelity.
/// </summary>
public class FileService : IFileService
{
    private readonly PrintlyDbContext _context;
    private readonly IStorageService _storage;

    public FileService(PrintlyDbContext context, IStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<List<FileResponse>> UploadFilesAsync(
        List<IFormFile> files,
        string? instructions,
        Guid? categoryId,
        Guid userId,
        Guid orgId)
    {
        var responses = new List<FileResponse>();

        foreach (var file in files)
        {
            // Validate file size (50 MB limit)
            const long maxBytes = 50 * 1024 * 1024;
            if (file.Length > maxBytes)
                throw new InvalidOperationException($"{file.FileName} exceeds the 50 MB limit.");

            // Stream the file bytes to storage (local disk or Azure Blob)
            await using var stream = file.OpenReadStream();
            var storagePath = await _storage.UploadAsync(stream, file.FileName, file.ContentType);

            // Create the FileRecord in the database
            var record = new FileRecord
            {
                OriginalFileName = file.FileName,
                StoragePath = storagePath,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                PrintingInstructions = instructions,
                CategoryId = categoryId,
                UserId = userId,
                OrgId = orgId,
                Status = FileStatus.Queued,
                PaymentStatus = PaymentStatus.Pending
            };

            _context.FileRecords.Add(record);

            // Every file gets a PrintQueueItem created at the same time.
            var queueItem = new PrintQueueItem
            {
                FileRecordId = record.Id,
                OrgId = orgId,
                Status = FileStatus.Queued,
                QueuedAt = DateTime.UtcNow
            };

            _context.PrintQueueItems.Add(queueItem);
            await _context.SaveChangesAsync();

            responses.Add(await MapToResponseAsync(record, orgId));
        }

        return responses;
    }

    public async Task<List<FileResponse>> GetUserFilesAsync(Guid userId, Guid orgId)
    {
        var files = await _context.FileRecords
            .Include(f => f.Category)
            .Include(f => f.User)
            .Include(f => f.QueueItem)
            .Where(f => f.UserId == userId && f.OrgId == orgId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var responses = new List<FileResponse>();
        foreach (var f in files)
            responses.Add(await MapToResponseAsync(f, orgId));

        return responses;
    }

    public async Task<List<FileResponse>> GetOrgFilesAsync(Guid orgId)
    {
        var files = await _context.FileRecords
            .Include(f => f.Category)
            .Include(f => f.User)
            .Include(f => f.QueueItem)
            .Where(f => f.OrgId == orgId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var responses = new List<FileResponse>();
        foreach (var f in files)
            responses.Add(await MapToResponseAsync(f, orgId));

        return responses;
    }

    public async Task<FileResponse> GetFileByIdAsync(Guid fileId, Guid userId, Guid orgId)
    {
        var file = await _context.FileRecords
            .Include(f => f.Category)
            .Include(f => f.User)
            .Include(f => f.QueueItem)
            .FirstOrDefaultAsync(f => f.Id == fileId && f.OrgId == orgId)
            ?? throw new InvalidOperationException("File not found.");

        return await MapToResponseAsync(file, orgId);
    }

    public async Task<(Stream stream, string contentType, string fileName)> DownloadFileAsync(Guid fileId, Guid userId, Guid orgId)
    {
        var file = await _context.FileRecords
            .FirstOrDefaultAsync(f => f.Id == fileId && f.OrgId == orgId)
            ?? throw new InvalidOperationException("File not found.");

        var stream = await _storage.DownloadAsync(file.StoragePath);
        return (stream, file.ContentType, file.OriginalFileName);
    }

    public async Task DeleteFileAsync(Guid fileId, Guid userId, Guid orgId)
    {
        var file = await _context.FileRecords
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId && f.OrgId == orgId)
            ?? throw new InvalidOperationException("File not found.");

        if (file.Status != FileStatus.Queued)
            throw new InvalidOperationException("Only queued files can be deleted.");

        _context.FileRecords.Remove(file);
        await _context.SaveChangesAsync();
    }

    public async Task<FileResponse> ConvertFileAsync(Guid fileId, Guid userId, Guid orgId)
    {
        return await GetFileByIdAsync(fileId, userId, orgId);
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private async Task<FileResponse> MapToResponseAsync(FileRecord file, Guid orgId)
    {
        int? queuePosition = null;
        if (file.Status == FileStatus.Queued)
        {
            queuePosition = await _context.FileRecords
                .CountAsync(f => f.OrgId == orgId && f.Status == FileStatus.Queued && f.CreatedAt < file.CreatedAt) + 1;
        }

        return new FileResponse
        {
            Id = file.Id,
            OriginalFileName = file.OriginalFileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.FileSizeBytes,
            PageCount = file.PageCount,
            Price = file.Price,
            PriceLocked = file.PriceLocked,
            PrintingInstructions = file.PrintingInstructions,
            Status = file.Status.ToString(),
            PaymentStatus = file.PaymentStatus.ToString(),
            CategoryName = file.Category?.Name,
            UploaderName = file.User?.FullName ?? string.Empty,
            CreatedAt = file.CreatedAt,
            QueuePosition = queuePosition
        };
    }
}