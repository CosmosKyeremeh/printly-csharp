using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Core.DTOs.Requests;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Controllers;

/// <summary>
/// Admin-only queue management.
///
/// [Authorize(Roles = "Admin,Superadmin")] means ASP.NET Core checks
/// the "role" claim in the JWT. If it's not Admin or Superadmin,
/// it returns 403 Forbidden before the action method even runs.
/// </summary>
[Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
public class QueueController : BaseApiController
{
    private readonly IQueueService _queueService;
    private readonly CurrentUserService _currentUser;

    public QueueController(IQueueService queueService, CurrentUserService currentUser)
    {
        _queueService = queueService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// GET /api/queue
    /// GET /api/queue?status=Queued
    ///
    /// [FromQuery] reads the value from the URL query string.
    /// e.g. /api/queue?status=Printing
    /// The parameter is optional — if omitted, returns the full queue.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetQueue([FromQuery] FileStatus? status)
        => ExecuteAsync(() => _queueService.GetQueueAsync(_currentUser.OrgId, status));

    /// <summary>
    /// PATCH /api/queue/{id}/status
    /// Body: { status: "Printing" }
    ///
    /// PATCH means partial update — we're only changing the status field,
    /// not replacing the entire resource (that would be PUT).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateFileStatusRequest request)
    {
        // Parse the string status from the request into our FileStatus enum.
        // Enum.Parse is case-sensitive by default — ignoreCase: true fixes that.
        if (!Enum.TryParse<FileStatus>(request.Status, ignoreCase: true, out var status))
            return Task.FromResult<IActionResult>(
                BadRequest(new { error = $"Invalid status: {request.Status}" }));

        return ExecuteAsync(() => _queueService.UpdateStatusAsync(
            id, status, _currentUser.UserId, _currentUser.OrgId));
    }

    /// <summary>
    /// PATCH /api/queue/bulk-status
    /// Body: { fileIds: [...], status: "Printing" }
    /// Updates multiple files at once.
    /// </summary>
    [HttpPatch("bulk-status")]
    public Task<IActionResult> BulkUpdateStatus([FromBody] BulkStatusUpdateRequest request)
    {
        if (!Enum.TryParse<FileStatus>(request.Status, ignoreCase: true, out var status))
            return Task.FromResult<IActionResult>(
                BadRequest(new { error = $"Invalid status: {request.Status}" }));

        return ExecuteAsync(() => _queueService.BulkUpdateStatusAsync(
            request.FileIds, status, _currentUser.UserId, _currentUser.OrgId));
    }

    /// <summary>
    /// PATCH /api/queue/{id}/price
    /// Body: { price: 5.00 }
    /// </summary>
    [HttpPatch("{id:guid}/price")]
    public Task<IActionResult> SetPrice(Guid id, [FromBody] SetPriceRequest request)
        => ExecuteAsync(() => _queueService.SetPriceAsync(
            id, request.Price, _currentUser.UserId, _currentUser.OrgId));
}
