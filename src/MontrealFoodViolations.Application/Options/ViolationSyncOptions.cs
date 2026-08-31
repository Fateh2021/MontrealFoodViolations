namespace MontrealFoodViolations.Application.Options;

public class ViolationSyncOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 24;
}
