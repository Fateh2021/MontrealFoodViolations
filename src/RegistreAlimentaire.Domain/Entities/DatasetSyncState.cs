namespace RegistreAlimentaire.Domain.Entities;

public class DatasetSyncState
{
    public int Id { get; set; }
    public string DatasetName { get; set; } = "MontrealViolations";
    public DateTimeOffset? LastSyncStartedAt { get; set; }
    public DateTimeOffset? LastSyncCompletedAt { get; set; }
    public string? LastHash { get; set; }
    public string? LastETag { get; set; }
    public string? LastModified { get; set; }
    public int? LastRowCount { get; set; }
    public int LastInsertedRows { get; set; }
    public int LastUpdatedRows { get; set; }
    public int LastUnchangedRows { get; set; }
    public int LastSkippedRows { get; set; }
    public string Status { get; set; } = "Idle";
    public string? LastError { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
}
