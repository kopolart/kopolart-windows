using System;

namespace Kopolart.Windows.Domain.Models
{
    public class DataExportBundle
    {
        public string AppName { get; set; } = "Kopolart";
        public int FormatVersion { get; set; } = 1;
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public string AppVersion { get; set; } = "";
        public AppSettings Data { get; set; } = new();
    }
}
