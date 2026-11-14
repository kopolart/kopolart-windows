using Kopolart.Windows.Domain.Constants;
using System;
using System.Diagnostics;
using System.IO;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Serves the offline page from a virtual-host-mapped local folder so it runs on a
    /// trusted origin. Pages loaded via NavigateToString live on about:blank, whose
    /// messages are rejected by the trusted-source check - this broke the retry button.
    /// </summary>
    public static class OfflinePageManager
    {
        private static readonly object ExtractLock = new();
        private static bool _assetsExtracted = false;

        private static readonly (string ResourceName, string FileName)[] Assets =
        {
            ("Kopolart.Windows.assets.web.offline.html", "offline.html"),
            ("Kopolart.Windows.assets.web.offline.css", "offline.css"),
            ("Kopolart.Windows.assets.web.offline.js", "offline.js")
        };

        public static string AssetsFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppConstants.Paths.AppDataFolderName,
                "OfflineAssets");

        /// <summary>
        /// Extracts embedded offline assets to disk once per process run.
        /// Returns the folder path, or null on failure.
        /// </summary>
        public static string? TryEnsureAssetsExtracted()
        {
            lock (ExtractLock)
            {
                if (_assetsExtracted && Directory.Exists(AssetsFolder))
                {
                    return AssetsFolder;
                }

                try
                {
                    var folder = AssetsFolder;
                    Directory.CreateDirectory(folder);

                    var assembly = typeof(OfflinePageManager).Assembly;
                    foreach (var (resourceName, fileName) in Assets)
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream == null)
                        {
                            Debug.WriteLine($"Offline asset resource '{resourceName}' not found.");
                            return null;
                        }

                        using var file = new FileStream(
                            Path.Combine(folder, fileName), FileMode.Create, FileAccess.Write, FileShare.None);
                        stream.CopyTo(file);
                    }

                    _assetsExtracted = true;
                    return folder;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to extract offline page assets: {ex}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Builds the offline page URL carrying theme and Persian localized strings.
        /// </summary>
        public static string BuildOfflineUrl(bool isDark)
        {
            string title = TextManager.GetString("OfflineTitle");
            string heading = TextManager.GetString("OfflineHeading");
            string text = TextManager.GetString("OfflineText");
            string button = TextManager.GetString("OfflineButton");
            string countdown = TextManager.GetString("OfflineCountdown");

            return AppConstants.Urls.OfflineBaseUrl + "offline.html" +
                   $"?dark={(isDark ? "1" : "0")}" +
                   $"&title={Uri.EscapeDataString(title)}" +
                   $"&heading={Uri.EscapeDataString(heading)}" +
                   $"&text={Uri.EscapeDataString(text)}" +
                   $"&button={Uri.EscapeDataString(button)}" +
                   $"&countdown={Uri.EscapeDataString(countdown)}";
        }

        /// <summary>
        /// Returns true when the given URI points at the offline virtual host.
        /// </summary>
        public static bool IsOfflinePageUri(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            {
                return false;
            }

            return parsed.Host.Equals(AppConstants.Urls.OfflineVirtualHost, StringComparison.OrdinalIgnoreCase);
        }
    }
}

