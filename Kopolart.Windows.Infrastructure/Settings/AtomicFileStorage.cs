using System;
using System.Diagnostics;
using System.IO;

namespace Kopolart.Windows.Infrastructure.Settings
{
    public class AtomicFileStorage
    {
        private readonly string _primaryFilePath;
        private readonly string _backupFilePath;

        public AtomicFileStorage(string primaryFilePath)
        {
            _primaryFilePath = primaryFilePath ?? throw new ArgumentNullException(nameof(primaryFilePath));
            _backupFilePath = primaryFilePath + ".bak";
        }

        public string? TryReadContent()
        {
            if (File.Exists(_primaryFilePath))
            {
                try
                {
                    return File.ReadAllText(_primaryFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read primary file '{_primaryFilePath}': {ex}");
                }
            }

            if (File.Exists(_backupFilePath))
            {
                try
                {
                    return File.ReadAllText(_backupFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read backup file '{_backupFilePath}': {ex}");
                }
            }

            return null;
        }

        public void WriteAtomic(string content)
        {
            var tempPath = _primaryFilePath + ".tmp";
            File.WriteAllText(tempPath, content);

            if (File.Exists(_primaryFilePath))
            {
                File.Replace(tempPath, _primaryFilePath, _backupFilePath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, _primaryFilePath);
            }
        }

        public string CreateSafetyBackup(string backupDirectory, string prefix = "pre_import_")
        {
            try
            {
                if (!File.Exists(_primaryFilePath))
                    return string.Empty;

                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                var backupPath = Path.Combine(backupDirectory, $"{prefix}{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                File.Copy(_primaryFilePath, backupPath, overwrite: true);
                return backupPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create safety backup: {ex}");
                return string.Empty;
            }
        }
    }
}
