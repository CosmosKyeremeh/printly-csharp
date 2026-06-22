using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Enums;

namespace Printly.Core.Interfaces;

/// <summary>
/// Manages the print queue - the state machine that moves files
/// from Queued to Printing to Done (or Cancelled).
/// Only admins interact with this service.
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Get the full queue for an org, with optional status filter.
    /// </summary>
    Task<List<QueueItemResponse>> GetQueueAsync(Guid orgId, FileStatus? filter = null);

    /// <summary>
    /// Move a single file to a new status.
    /// Validates the transition is legal before applying it.
    /// </summary>
    Task<QueueItemResponse> UpdateStatusAsync(
        Guid fileId,
        FileStatus newStatus,
        Guid adminUserId,
        Guid orgId);

    /// <summary>
    /// Move multiple files to a new status in one operation.
    /// Used for the "bulk select and mark as Printing" feature.
    /// </summary>
    Task BulkUpdateStatusAsync(
        List<Guid> fileIds,
        FileStatus newStatus,
        Guid adminUserId,
        Guid orgId);

    /// <summary>
    /// Set or override the price for a file and lock it.
    /// Once locked, auto-calculation won't change the price again.
    /// </summary>
    Task<QueueItemResponse> SetPriceAsync(
        Guid fileId,
        decimal price,
        Guid adminUserId,
        Guid orgId);
}
