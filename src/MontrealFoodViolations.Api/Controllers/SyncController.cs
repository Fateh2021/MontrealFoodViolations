using Microsoft.AspNetCore.Http;
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
    [EndpointSummary("Lance une synchronisation manuelle du CSV officiel.")]
    public async Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
    {
        var result = await _syncService.SyncAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("sync/status")]
    [EndpointSummary("État de la dernière synchronisation.")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var status = await _syncService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }
}
