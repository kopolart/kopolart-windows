namespace Kopolart.Windows.Domain.Models
{
    public record DownloadProgressReport(long BytesReceived, long? TotalBytes, int Percentage);
}
