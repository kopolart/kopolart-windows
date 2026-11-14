using System.IO;
using Kopolart.Windows.Domain.Models;

namespace Kopolart.Windows.Domain.Helpers
{
    public static class WallpaperEligibility
    {
        public static bool IsCandidate(DownloadedItem? item, bool fileExists)
        {
            if (item == null || !fileExists)
                return false;

            if (item.Category != DownloadCategory.Wallpaper)
                return false;

            return IsSupportedImage(string.IsNullOrWhiteSpace(item.FilePath) ? item.FileName : item.FilePath);
        }

        public static bool IsSupportedImage(string? pathOrName)
        {
            if (string.IsNullOrWhiteSpace(pathOrName))
                return false;

            string extension = Path.GetExtension(pathOrName).ToLowerInvariant();

            // Animated and vector formats cannot be used as a desktop wallpaper.
            if (extension is ".gif" or ".svg")
                return false;

            return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp" or ".heic" or ".heif";
        }
    }
}
