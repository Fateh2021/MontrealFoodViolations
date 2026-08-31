using MontrealFoodViolations.Application.Interfaces;

namespace MontrealFoodViolations.Infrastructure.Services;

public sealed class DatasetSyncCoordinator : IDatasetSyncCoordinator
{
    public SemaphoreSlim SyncLock { get; } = new(1, 1);
}
