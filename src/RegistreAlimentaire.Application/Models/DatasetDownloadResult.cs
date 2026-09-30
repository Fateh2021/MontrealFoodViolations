namespace RegistreAlimentaire.Application.Models;

public sealed class DatasetDownloadResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string Hash { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public string? LastModified { get; set; }
    public long? ContentLength { get; set; }
}
