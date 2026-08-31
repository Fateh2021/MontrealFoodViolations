using MontrealFoodViolations.Application.Models;

namespace MontrealFoodViolations.Application.Interfaces;

public interface IViolationSyncService
{
    Task<ViolationSyncResult> SyncAsync(CancellationToken cancellationToken = default);
    Task<SyncStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default);
}
