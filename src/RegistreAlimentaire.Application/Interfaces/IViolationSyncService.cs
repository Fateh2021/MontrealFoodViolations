using RegistreAlimentaire.Application.Models;

namespace RegistreAlimentaire.Application.Interfaces;

public interface IViolationSyncService
{
    Task<ViolationSyncResult> SyncAsync(CancellationToken cancellationToken = default);
    Task<SyncStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default);
}
