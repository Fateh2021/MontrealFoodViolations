namespace MontrealFoodViolations.Application.Interfaces;

public interface IDatasetSyncCoordinator
{
    SemaphoreSlim SyncLock { get; }
}
