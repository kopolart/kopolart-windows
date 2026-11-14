using System;
using Kopolart.Windows.Domain.Helpers;

namespace Kopolart.Windows.Domain.Models
{
    public class VisitedPage
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime VisitedAt { get; set; } = DateTime.Now;

        private string? _category;
        public string Category
        {
            get => !string.IsNullOrWhiteSpace(_category) && _category != "Other"
                ? ContentCategoryHelper.NormalizeCategory(_category, Url)
                : ContentCategoryHelper.DetermineCategoryString(Url);
            set => _category = value;
        }
    }
}
