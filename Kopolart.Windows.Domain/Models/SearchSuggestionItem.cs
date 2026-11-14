using System;
using System.Text.Json.Serialization;

namespace Kopolart.Windows.Domain.Models
{
    public class SearchSuggestionItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonIgnore]
        public string FullImageUrl { get; set; } = string.Empty;

        [JsonIgnore]
        public string FullTargetUrl { get; set; } = string.Empty;

        [JsonIgnore]
        public bool HasImage => !string.IsNullOrWhiteSpace(FullImageUrl);

        [JsonIgnore]
        public bool HasValueText =>
            !string.IsNullOrWhiteSpace(Value) &&
            !string.Equals(Value.Trim(), Label?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
