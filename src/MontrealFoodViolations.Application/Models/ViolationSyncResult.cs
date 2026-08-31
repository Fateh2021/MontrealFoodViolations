namespace MontrealFoodViolations.Application.Models;

public class ViolationSyncResult
{
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public bool DownloadSucceeded { get; set; }
    public int TotalRows { get; set; }
    public int InsertedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int UnchangedRows { get; set; }
    public int SkippedRows { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DatasetHash { get; set; }
}
