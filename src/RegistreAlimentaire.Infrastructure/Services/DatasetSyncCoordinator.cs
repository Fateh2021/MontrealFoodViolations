using RegistreAlimentaire.Application.Interfaces;

namespace RegistreAlimentaire.Infrastructure.Services;

public sealed class DatasetSyncCoordinator : IDatasetSyncCoordinator
{
    public SemaphoreSlim SyncLock { get; } = new(1, 1);
}
