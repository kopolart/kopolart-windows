using System;
using Kopolart.Windows.Domain.Helpers;

namespace Kopolart.Windows.Domain.Models
{
    public class BookmarkItem
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime DateAdded { get; set; } = DateTime.Now;

        private string? _category;
        public string Category
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_category) && _category != "Other")
                {
                    return ContentCategoryHelper.NormalizeCategory(_category, Url);
                }
                return ContentCategoryHelper.DetermineCategoryString(Url);
            }
            set => _category = value;
        }

        public override string ToString() => Title;

        public static string DetermineCategory(string url)
        {
            return ContentCategoryHelper.DetermineCategoryString(url);
        }
    }
}
