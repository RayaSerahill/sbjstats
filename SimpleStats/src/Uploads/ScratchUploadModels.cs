namespace sbjStats;

public sealed class ScratchUploadRequest
{
    public string UploadType { get; set; } = string.Empty;
    public string RawJson { get; set; } = string.Empty;
    public string? GameId { get; set; }
    public string? PlayerName { get; set; }
    public long? OccurredAtUnixSeconds { get; set; }
}
