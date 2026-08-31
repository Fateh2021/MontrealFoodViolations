namespace MontrealFoodViolations.Application.Models;

public sealed class SyncStatusSnapshot
{
    public DateTimeOffset? LastSyncCompletedAt { get; set; }
    public string Status { get; set; } = "Idle";
    public TimeSpan? Duration { get; set; }
    public int ProcessedRows { get; set; }
    public int InsertedRows { get; set; }
    public int UpdatedRows { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? NextScheduledRun { get; set; }
}
