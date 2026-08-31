using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MontrealFoodViolations.Application.Interfaces;
using MontrealFoodViolations.Application.Models;
using MontrealFoodViolations.Application.Options;

namespace MontrealFoodViolations.Infrastructure.Services;

public sealed class MontrealDatasetClient : IMontrealDatasetClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MontrealDatasetClient> _logger;
    private readonly MontrealDatasetOptions _options;

    public MontrealDatasetClient(IHttpClientFactory httpClientFactory, IOptions<MontrealDatasetOptions> options, ILogger<MontrealDatasetClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("MontrealDataset");
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(15, _options.TimeoutSeconds));
    }

    public async Task<DatasetDownloadResult> DownloadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading Montreal violations dataset from {Url}", _options.ViolationsUrl);

        using var response = await _httpClient.GetAsync(_options.ViolationsUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = $"HTTP {(int)response.StatusCode} while downloading dataset.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(contentBytes)).ToLowerInvariant();

        var result = new DatasetDownloadResult
        {
            Content = contentBytes,
            Hash = hash,
            ETag = response.Headers.TryGetValues("ETag", out var etagValues) ? etagValues.FirstOrDefault() : null,
            LastModified = response.Headers.TryGetValues("Last-Modified", out var modifiedValues) ? modifiedValues.FirstOrDefault() : null,
            ContentLength = contentBytes.Length
        };

        _logger.LogInformation("Dataset downloaded successfully. Size {ContentLength} bytes, SHA256 {Hash}", result.ContentLength, result.Hash);
        return result;
    }
}
