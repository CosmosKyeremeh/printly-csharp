using Microsoft.AspNetCore.Mvc;

namespace Printly.Web.Controllers;

/// <summary>
/// All API controllers inherit from this.
/// It provides a single ExecuteAsync helper that:
///   1. Runs the action
///   2. Returns 200 OK with the result on success
///   3. Catches InvalidOperationException ? 400 Bad Request
///   4. Catches any other exception ? 500 Internal Server Error
///
/// This means individual controllers never need try/catch blocks —
/// error handling is defined once here and inherited everywhere.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // Business logic errors — wrong join code, file not found, etc.
            // Return 400 so the client knows it sent something wrong.
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Unexpected errors — database down, storage failure, etc.
            // Return 500 so the client knows it's a server problem.
            return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    protected async Task<IActionResult> ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
        }
    }
}
