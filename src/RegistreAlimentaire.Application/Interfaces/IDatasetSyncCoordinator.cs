namespace RegistreAlimentaire.Application.Interfaces;

public interface IDatasetSyncCoordinator
{
    SemaphoreSlim SyncLock { get; }
}
