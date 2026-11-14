using System;

namespace Kopolart.Windows.Domain.Models
{
    public class DataImportSummary
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int BookmarksCount { get; set; }
        public int HistoryCount { get; set; }
        public int DownloadsCount { get; set; }
        public int UsageSessionsCount { get; set; }
        public DateTime? ExportedAt { get; set; }
        public string? AppVersion { get; set; }
        public DataImportMode Mode { get; set; }
    }
}
