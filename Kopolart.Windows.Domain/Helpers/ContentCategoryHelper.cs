using Kopolart.Windows.Domain.Models;

namespace Kopolart.Windows.Domain.Helpers
{
    public static class ContentCategoryHelper
    {
        public static ContentCategory DetermineCategory(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return ContentCategory.Other;

            var lowerUrl = url.ToLowerInvariant();

            if (lowerUrl.Contains("/colors/") || lowerUrl.Contains("/color-palettes/"))
                return ContentCategory.Color;

            if (lowerUrl.Contains("/posts/"))
                return ContentCategory.Post;

            if (lowerUrl.Contains("/tools/"))
                return ContentCategory.Tools;

            if (lowerUrl.Contains("/wallpapers/"))
                return ContentCategory.Wallpaper;

            return ContentCategory.Other;
        }

        public static string DetermineCategoryString(string? url)
        {
            return ToCategoryString(DetermineCategory(url));
        }

        public static string ToCategoryString(ContentCategory category)
        {
            return category switch
            {
                ContentCategory.Wallpaper => "Wallpaper",
                ContentCategory.Post => "Post",
                ContentCategory.Color => "Color",
                ContentCategory.Tools => "Tools",
                _ => "Other"
            };
        }

        public static ContentCategory ParseCategory(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return ContentCategory.Other;

            return categoryName.Trim().ToLowerInvariant() switch
            {
                "wallpaper" or "wallpapers" => ContentCategory.Wallpaper,
                "post" or "posts" or "artworks" or "artwork" => ContentCategory.Post,
                "color" or "colors" or "palette" or "palettes" or "colorpalette" or "color-palette" or "colorpalettes" => ContentCategory.Color,
                "tools" or "tool" => ContentCategory.Tools,
                _ => ContentCategory.Other
            };
        }

        public static string NormalizeCategory(string? category, string? url = null)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                var fromUrl = DetermineCategory(url);
                if (fromUrl != ContentCategory.Other)
                    return ToCategoryString(fromUrl);
            }

            return ToCategoryString(ParseCategory(category));
        }
    }
}
