using Kopolart.Windows.Domain.Models;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IDataBackupService
    {
        /// <summary>
        /// Exports all application settings, bookmarks, downloads, history, logs, and milestones into a JSON file.
        /// </summary>
        Task<bool> ExportToFileAsync(string filePath, string appVersion);

        /// <summary>
        /// Reads and validates a backup JSON file before importing.
        /// </summary>
        Task<(bool IsValid, DataExportBundle? Bundle, string? ErrorMessage)> PreviewBackupFileAsync(string filePath);

        /// <summary>
        /// Imports application data from a backup JSON file according to the chosen mode (ReplaceAll or Merge).
        /// Takes an automatic pre-import safety backup first.
        /// </summary>
        Task<DataImportSummary> ImportFromFileAsync(string filePath, DataImportMode mode);
    }
}
