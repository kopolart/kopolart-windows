namespace Kopolart.Windows.Domain.Models
{
    public class SearchCategoryOption
    {
        public SearchCategoryType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortTitle { get; set; } = string.Empty;
        public string SearchUrl { get; set; } = string.Empty;
        public string ApiPart { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public int AccentColorLightArgb { get; set; }
        public int AccentColorDarkArgb { get; set; }
    }
}
