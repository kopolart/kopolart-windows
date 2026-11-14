using Kopolart.Windows.Domain.Constants;
using System;
using System.Diagnostics;
using System.IO;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Persists the version an in-flight self-update is expected to install, so the
    /// relaunched app can verify the swap actually succeeded before reporting success.
    /// </summary>
    public static class UpdateMarker
    {
        private static string MarkerFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.Paths.AppDataFolderName,
            "pending-update-version.txt");

        public static void Save(string expectedVersion)
        {
            try
            {
                var path = MarkerFilePath;
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(path, expectedVersion ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write update marker: {ex}");
            }
        }

        public static string? ReadAndClear()
        {
            try
            {
                var path = MarkerFilePath;
                if (!File.Exists(path))
                {
                    return null;
                }

                var version = File.ReadAllText(path).Trim();
                File.Delete(path);
                return version.Length > 0 ? version : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read update marker: {ex}");
                return null;
            }
        }
    }
}
