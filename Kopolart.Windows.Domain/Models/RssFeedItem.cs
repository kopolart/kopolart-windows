using System;

namespace Kopolart.Windows.Domain.Models
{
    /// <summary>
    /// Represents an article, wallpaper, or design post item fetched from the KopolArt RSS feed.
    /// </summary>
    public class RssFeedItem
    {
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? PublishDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public string Guid { get; set; } = string.Empty;
    }
}
