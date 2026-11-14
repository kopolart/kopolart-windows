namespace Kopolart.Windows.Domain.Models
{
    public enum WallpaperSetStatus
    {
        Success = 0,
        FileMissing = 1,
        UnsupportedFormat = 2,
        ConversionFailed = 3,
        Failed = 4
    }

    public class WallpaperSetResult
    {
        public WallpaperSetStatus Status { get; init; } = WallpaperSetStatus.Failed;
        public string? DiagnosticMessage { get; init; }

        public bool Succeeded => Status == WallpaperSetStatus.Success;

        public static WallpaperSetResult Success() => new() { Status = WallpaperSetStatus.Success };

        public static WallpaperSetResult Fail(WallpaperSetStatus status, string? diagnosticMessage = null)
            => new() { Status = status, DiagnosticMessage = diagnosticMessage };
    }
}
