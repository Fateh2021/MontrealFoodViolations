using MontrealFoodViolations.Application.Models;

namespace MontrealFoodViolations.Application.Interfaces;

public interface IMontrealDatasetClient
{
    Task<DatasetDownloadResult> DownloadAsync(CancellationToken cancellationToken = default);
}
