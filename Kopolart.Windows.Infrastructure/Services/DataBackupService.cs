using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class DataBackupService : IDataBackupService
    {
        private static readonly JsonSerializerOptions IndentedJsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ISettingsManager _settingsManager;

        public DataBackupService(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        }

        public async Task<bool> ExportToFileAsync(string filePath, string appVersion)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var snapshot = _settingsManager.GetSettingsSnapshot();
                // Ensure machine-local screen coordinates are omitted
                snapshot.WindowState = 0;
                snapshot.WindowLocation = string.Empty;
                snapshot.WindowSize = string.Empty;

                var bundle = new DataExportBundle
                {
                    AppName = "Kopolart",
                    FormatVersion = 1,
                    ExportedAt = DateTime.UtcNow,
                    AppVersion = appVersion ?? string.Empty,
                    Data = snapshot
                };

                string json = JsonSerializer.Serialize(bundle, IndentedJsonOptions);
                var tempPath = filePath + ".tmp";

                await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8);

                if (File.Exists(filePath))
                {
                    File.Replace(tempPath, filePath, null);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataBackupService] Export failed: {ex}");
                return false;
            }
        }

        public async Task<(bool IsValid, DataExportBundle? Bundle, string? ErrorMessage)> PreviewBackupFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return (false, null, "File does not exist.");
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return (false, null, "File is empty.");
                }
                if (fileInfo.Length > 50 * 1024 * 1024) // 50 MB limit
                {
                    return (false, null, "File is too large to be a valid backup file.");
                }

                string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

                // Try Format V1 (Envelope)
                try
                {
                    var bundle = JsonSerializer.Deserialize<DataExportBundle>(json, CaseInsensitiveOptions);
                    if (bundle?.Data != null && (!string.IsNullOrWhiteSpace(bundle.AppName) || bundle.FormatVersion > 0))
                    {
                        return (true, bundle, null);
                    }
                }
                catch
                {
                    // Fall back to raw AppSettings
                }

                // Try Format Legacy (Raw AppSettings)
                try
                {
                    var raw = JsonSerializer.Deserialize<AppSettings>(json, CaseInsensitiveOptions);
                    if (raw != null && (raw.Bookmarks != null || raw.NavigationHistory != null || !string.IsNullOrWhiteSpace(raw.LastAppVersion)))
                    {
                        var fallbackBundle = new DataExportBundle
                        {
                            AppName = "Kopolart",
                            FormatVersion = 0,
                            ExportedAt = File.GetLastWriteTimeUtc(filePath),
                            AppVersion = raw.LastAppVersion ?? "Legacy",
                            Data = raw
                        };
                        return (true, fallbackBundle, null);
                    }
                }
                catch (Exception ex)
                {
                    return (false, null, $"Invalid JSON syntax: {ex.Message}");
                }

                return (false, null, "Unrecognized backup file format.");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<DataImportSummary> ImportFromFileAsync(string filePath, DataImportMode mode)
        {
            var preview = await PreviewBackupFileAsync(filePath);
            if (!preview.IsValid || preview.Bundle?.Data == null)
            {
                return new DataImportSummary
                {
                    Success = false,
                    ErrorMessage = preview.ErrorMessage ?? "Failed to read backup file."
                };
            }

            try
            {
                // Take pre-import safety backup
                _settingsManager.CreateSafetyBackup();

                var bundle = preview.Bundle;
                _settingsManager.ApplySettingsSnapshot(bundle.Data, mode);

                return new DataImportSummary
                {
                    Success = true,
                    Mode = mode,
                    ExportedAt = bundle.ExportedAt,
                    AppVersion = bundle.AppVersion,
                    BookmarksCount = bundle.Data.Bookmarks?.Count ?? 0,
                    HistoryCount = bundle.Data.NavigationHistory?.Count ?? 0,
                    DownloadsCount = bundle.Data.DownloadHistory?.Count ?? 0,
                    UsageSessionsCount = bundle.Data.UsageSessions?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataBackupService] Import failed: {ex}");
                return new DataImportSummary
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
