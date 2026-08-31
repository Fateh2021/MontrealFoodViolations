using Microsoft.AspNetCore.Mvc;
using MontrealFoodViolations.Application.Interfaces;

namespace MontrealFoodViolations.Api.Controllers;

[ApiController]
[Route("api")]
public class SyncController : ControllerBase
{
    private readonly IViolationSyncService _syncService;

    public SyncController(IViolationSyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
    {
        var result = await _syncService.SyncAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("sync/status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var status = await _syncService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }
}
