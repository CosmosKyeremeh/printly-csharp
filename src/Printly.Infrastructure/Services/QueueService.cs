using Microsoft.EntityFrameworkCore;
using Printly.Core.DTOs.Responses;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;

namespace Printly.Infrastructure.Services;

/// <summary>
/// Manages the print queue state machine.
///
/// STATE MACHINE concept:
/// A state machine is a system that can be in exactly one "state" at a time
/// and transitions between states only via defined, legal operations.
/// We enforce this by checking the CURRENT state before allowing a transition.
/// </summary>
public class QueueService : IQueueService
{
    private readonly PrintlyDbContext _context;

    public QueueService(PrintlyDbContext context)
    {
        _context = context;
    }

    public async Task<List<QueueItemResponse>> GetQueueAsync(
        Guid orgId, FileStatus? filter = null)
    {
        var query = _context.PrintQueueItems
            .Include(q => q.FileRecord)
                .ThenInclude(f => f.User)
            .Include(q => q.FileRecord)
                .ThenInclude(f => f.Category)
            .Where(q => q.OrgId == orgId);

        // ThenInclude lets us go two levels deep in navigation properties.
        // q.FileRecord.User means: load the PrintQueueItem,
        // then load its FileRecord, then load that FileRecord's User.

        if (filter.HasValue)
            query = query.Where(q => q.Status == filter.Value);

        var items = await query
            .OrderBy(q => q.QueuedAt)
            .ToListAsync();

        return items.Select(q => new QueueItemResponse
        {
            Id = q.Id,
            FileRecordId = q.FileRecordId,
            FileName = q.FileRecord.OriginalFileName,
            UploaderName = q.FileRecord.User?.FullName ?? string.Empty,
            UploaderWhatsApp = q.FileRecord.User?.WhatsAppNumber,
            Status = q.Status.ToString(),
            PaymentStatus = q.FileRecord.PaymentStatus.ToString(),
            Price = q.FileRecord.Price,
            PriceLocked = q.FileRecord.PriceLocked,
            PageCount = q.FileRecord.PageCount,
            PrintingInstructions = q.FileRecord.PrintingInstructions,
            CategoryName = q.FileRecord.Category?.Name,
            QueuedAt = q.QueuedAt,
            PrintingStartedAt = q.PrintingStartedAt,
            PrintedAt = q.PrintedAt
        }).ToList();
    }

    public async Task<QueueItemResponse> UpdateStatusAsync(
        Guid fileId,
        FileStatus newStatus,
        Guid adminUserId,
        Guid orgId)
    {
        var queueItem = await _context.PrintQueueItems
            .Include(q => q.FileRecord)
                .ThenInclude(f => f.User)
            .Include(q => q.FileRecord)
                .ThenInclude(f => f.Category)
            .FirstOrDefaultAsync(q =>
                q.FileRecordId == fileId && q.OrgId == orgId)
            ?? throw new InvalidOperationException("Queue item not found.");

        // Validate the transition is legal before applying it.
        ValidateTransition(queueItem.Status, newStatus);

        var now = DateTime.UtcNow;

        // Update both the QueueItem and the FileRecord status together.
        // They must always stay in sync.
        queueItem.Status = newStatus;
        queueItem.ActionedByUserId = adminUserId;
        queueItem.FileRecord.Status = newStatus;

        // Record the exact timestamp of each transition.
        switch (newStatus)
        {
            case FileStatus.Printing:
                queueItem.PrintingStartedAt = now;
                break;
            case FileStatus.Done:
                queueItem.PrintedAt = now;
                break;
            case FileStatus.Cancelled:
                queueItem.CancelledAt = now;
                break;
        }

        await _context.SaveChangesAsync();

        return new QueueItemResponse
        {
            Id = queueItem.Id,
            FileRecordId = queueItem.FileRecordId,
            FileName = queueItem.FileRecord.OriginalFileName,
            UploaderName = queueItem.FileRecord.User?.FullName ?? string.Empty,
            UploaderWhatsApp = queueItem.FileRecord.User?.WhatsAppNumber,
            Status = queueItem.Status.ToString(),
            PaymentStatus = queueItem.FileRecord.PaymentStatus.ToString(),
            Price = queueItem.FileRecord.Price,
            PriceLocked = queueItem.FileRecord.PriceLocked,
            PageCount = queueItem.FileRecord.PageCount,
            PrintingInstructions = queueItem.FileRecord.PrintingInstructions,
            CategoryName = queueItem.FileRecord.Category?.Name,
            QueuedAt = queueItem.QueuedAt,
            PrintingStartedAt = queueItem.PrintingStartedAt,
            PrintedAt = queueItem.PrintedAt
        };
    }

    public async Task BulkUpdateStatusAsync(
        List<Guid> fileIds,
        FileStatus newStatus,
        Guid adminUserId,
        Guid orgId)
    {
        // Load all matching queue items in one database round-trip.
        // One query for N files is far more efficient than N separate queries.
        var items = await _context.PrintQueueItems
            .Include(q => q.FileRecord)
            .Where(q => fileIds.Contains(q.FileRecordId) && q.OrgId == orgId)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var item in items)
        {
            // Skip items where the transition isn't legal
            // rather than throwing — bulk operations are best-effort.
            if (!IsValidTransition(item.Status, newStatus)) continue;

            item.Status = newStatus;
            item.FileRecord.Status = newStatus;
            item.ActionedByUserId = adminUserId;

            if (newStatus == FileStatus.Printing) item.PrintingStartedAt = now;
            if (newStatus == FileStatus.Done) item.PrintedAt = now;
            if (newStatus == FileStatus.Cancelled) item.CancelledAt = now;
        }

        // SaveChangesAsync sends ONE UPDATE statement per modified entity.
        // EF Core tracks which objects changed (the Change Tracker)
        // and generates efficient SQL for only those rows.
        await _context.SaveChangesAsync();
    }

    public async Task<QueueItemResponse> SetPriceAsync(
        Guid fileId,
        decimal price,
        Guid adminUserId,
        Guid orgId)
    {
        var queueItem = await _context.PrintQueueItems
            .Include(q => q.FileRecord)
                .ThenInclude(f => f.User)
            .FirstOrDefaultAsync(q =>
                q.FileRecordId == fileId && q.OrgId == orgId)
            ?? throw new InvalidOperationException("Queue item not found.");

        queueItem.FileRecord.Price = price;
        queueItem.FileRecord.PriceLocked = true;

        await _context.SaveChangesAsync();

        return new QueueItemResponse
        {
            Id = queueItem.Id,
            FileRecordId = queueItem.FileRecordId,
            FileName = queueItem.FileRecord.OriginalFileName,
            UploaderName = queueItem.FileRecord.User?.FullName ?? string.Empty,
            Status = queueItem.Status.ToString(),
            PaymentStatus = queueItem.FileRecord.PaymentStatus.ToString(),
            Price = queueItem.FileRecord.Price,
            PriceLocked = queueItem.FileRecord.PriceLocked,
            PageCount = queueItem.FileRecord.PageCount,
            QueuedAt = queueItem.QueuedAt
        };
    }

    // -- State Machine Validation -------------------------------------------

    /// <summary>
    /// Defines which transitions are legal.
    /// Queued ? Printing ?
    /// Printing ? Done   ?
    /// Queued ? Cancelled ?
    /// Done ? Queued      ? (can't un-print something)
    /// </summary>
    private static bool IsValidTransition(FileStatus current, FileStatus next)
    {
        return (current, next) switch
        {
            (FileStatus.Queued,    FileStatus.Printing)  => true,
            (FileStatus.Queued,    FileStatus.Cancelled) => true,
            (FileStatus.Printing,  FileStatus.Done)      => true,
            (FileStatus.Printing,  FileStatus.Cancelled) => true,
            _ => false
        };
    }

    private static void ValidateTransition(FileStatus current, FileStatus next)
    {
        if (!IsValidTransition(current, next))
            throw new InvalidOperationException(
                $"Cannot transition from {current} to {next}.");
    }
}
