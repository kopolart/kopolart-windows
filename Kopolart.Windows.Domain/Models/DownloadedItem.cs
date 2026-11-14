using System;
using System.Text.Json.Serialization;

namespace Kopolart.Windows.Domain.Models
{
    public class DownloadedItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Url { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public DateTime DownloadedAt { get; set; } = DateTime.Now;

        private DownloadCategory _category = DownloadCategory.Other;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DownloadCategory Category
        {
            get
            {
                var determined = DetermineCategory(Url, FileName);
                if (determined != DownloadCategory.Other)
                    return determined;
                if (_category == DownloadCategory.Tools)
                    return DownloadCategory.Other;
                return _category;
            }
            set => _category = value;
        }

        public static DownloadCategory DetermineCategory(string? url, string? fileName = null)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                var lowerUrl = url.ToLowerInvariant();

                // 1. Colors (رنگ)
                if (lowerUrl.Contains("/color-swatches/") ||
                    lowerUrl.Contains("/solid-color-wallpapers/") ||
                    lowerUrl.Contains("images/palettes/") ||
                    lowerUrl.Contains("/color-palettes-wallpaper/") ||
                    lowerUrl.Contains("/color-palette-image/") ||
                    lowerUrl.Contains("/color-cards/") ||
                    lowerUrl.Contains("/solid-color-custom-wallpaper/") ||
                    lowerUrl.Contains("/custom-color-image/") ||
                    lowerUrl.Contains("/custom-palette-image/") ||
                    lowerUrl.Contains("/colors/") ||
                    lowerUrl.Contains("/color-palettes/"))
                {
                    return DownloadCategory.Color;
                }

                // 2. Wallpapers (والپیپر)
                if (lowerUrl.Contains("/wallpapers/previews/") ||
                    lowerUrl.Contains("/images/wallpapers/"))
                {
                    return DownloadCategory.Wallpaper;
                }

                // 3. Posts (پست)
                if (lowerUrl.Contains("/images/posts/downloads/"))
                {
                    return DownloadCategory.Post;
                }
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var lowerFile = fileName.ToLowerInvariant();

                if (lowerFile.Contains("color-swatches") ||
                    lowerFile.Contains("solid-color-wallpaper") ||
                    lowerFile.Contains("palette") ||
                    lowerFile.Contains("color-cards") ||
                    lowerFile.Contains("custom-color") ||
                    lowerFile.Contains("custom-palette"))
                {
                    return DownloadCategory.Color;
                }

                if (lowerFile.Contains("wallpaper"))
                {
                    return DownloadCategory.Wallpaper;
                }

                if (lowerFile.Contains("post"))
                {
                    return DownloadCategory.Post;
                }
            }

            return DownloadCategory.Other;
        }
    }
}
