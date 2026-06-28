using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Controllers;

/// <summary>
/// Handles all file operations for students and admins.
///
/// [Authorize] on the class means EVERY action in this controller
/// requires a valid JWT. No unauthenticated requests get through.
/// ASP.NET Core reads the JWT from the Authorization header:
///   Authorization: Bearer eyJhbGciOiJIUzI1NiJ9...
/// </summary>
[Authorize]
public class FilesController : BaseApiController
{
    private readonly IFileService _fileService;
    private readonly CurrentUserService _currentUser;

    public FilesController(IFileService fileService, CurrentUserService currentUser)
    {
        _fileService = fileService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// POST /api/files/upload
    /// Content-Type: multipart/form-data
    ///
    /// [FromForm] tells ASP.NET Core to read this from a multipart form
    /// submission rather than a JSON body — required for file uploads.
    /// IFormFileCollection is ASP.NET Core's representation of uploaded files.
    /// </summary>
    [HttpPost("upload")]
    public Task<IActionResult> Upload(
        [FromForm] IFormFileCollection files,
        [FromForm] string? instructions,
        [FromForm] Guid? categoryId)
        => ExecuteAsync(() => _fileService.UploadFilesAsync(
            files.ToList(),
            instructions,
            categoryId,
            _currentUser.UserId,
            _currentUser.OrgId));

    /// <summary>
    /// GET /api/files
    /// Students see only their own files.
    /// Admins see all files in the org.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetFiles()
        => ExecuteAsync(() => _currentUser.IsAdmin
            ? _fileService.GetOrgFilesAsync(_currentUser.OrgId)
            : _fileService.GetUserFilesAsync(_currentUser.UserId, _currentUser.OrgId));

    /// <summary>
    /// GET /api/files/{id}
    /// {id} is a route parameter — ASP.NET Core extracts it from the URL.
    /// e.g. GET /api/files/3f2504e0-4f89-11d3-9a0c-0305e82c3301
    /// </summary>
    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetFile(Guid id)
        => ExecuteAsync(() => _fileService.GetFileByIdAsync(
            id, _currentUser.UserId, _currentUser.OrgId));

    /// <summary>
    /// GET /api/files/{id}/download
    /// Returns the raw file bytes as a downloadable stream.
    ///
    /// This action can't use ExecuteAsync because it returns a FileResult,
    /// not a data object — so we handle it directly.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var (stream, contentType, fileName) = await _fileService.DownloadFileAsync(
                id, _currentUser.UserId, _currentUser.OrgId);

            // File() tells ASP.NET Core to stream the bytes to the client
            // with the correct Content-Type and a suggested filename.
            return File(stream, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/files/{id}
    /// Soft-deletes the file (sets DeletedAt). Only works while Queued.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => ExecuteAsync(() => _fileService.DeleteFileAsync(
            id, _currentUser.UserId, _currentUser.OrgId));

    /// <summary>
    /// POST /api/files/{id}/convert
    /// Triggers PDF?DOCX conversion via ConvertAPI.
    /// </summary>
    [HttpPost("{id:guid}/convert")]
    public Task<IActionResult> Convert(Guid id)
        => ExecuteAsync(() => _fileService.ConvertFileAsync(
            id, _currentUser.UserId, _currentUser.OrgId));
}
