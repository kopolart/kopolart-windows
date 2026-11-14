using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class SearchSuggestionService : ISearchSuggestionService
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);
        private const int MinTermLength = 2;

        private readonly HttpClient _httpClient;
        private readonly ILogger<SearchSuggestionService>? _logger;
        private readonly ConcurrentDictionary<string, (DateTime CachedAt, List<SearchSuggestionItem> Items)> _cache = new(StringComparer.OrdinalIgnoreCase);

        public SearchSuggestionService(IHttpClientFactory? httpClientFactory = null, ILogger<SearchSuggestionService>? logger = null)
        {
            _httpClient = httpClientFactory?.CreateClient("DefaultClient") ?? new HttpClient { Timeout = RequestTimeout };
            _logger = logger;
        }

        public async Task<List<SearchSuggestionItem>> GetSuggestionsAsync(
            string term,
            SearchCategoryType category,
            CancellationToken cancellationToken = default)
        {
            var cleanTerm = term?.Trim() ?? string.Empty;
            if (cleanTerm.Length < MinTermLength)
            {
                return [];
            }

            var part = MapCategoryToPart(category);
            var cacheKey = $"{part}:{cleanTerm}";

            // 1. Check in-memory cache
            if (_cache.TryGetValue(cacheKey, out var cached) &&
                (DateTime.UtcNow - cached.CachedAt) < CacheDuration)
            {
                return cached.Items;
            }

            // 2. Connectivity check - fail fast without blocking UI
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    return [];
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "NetworkInterface check skipped.");
            }

            // 3. Make HTTP request to website API
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(RequestTimeout);

                var encodedTerm = Uri.EscapeDataString(cleanTerm);
                var baseHost = AppConstants.Urls.MainUrl.TrimEnd('/');
                var requestUrl = $"{AppConstants.Urls.SearchApi}?part={part}&term={encodedTerm}";

                using var response = await _httpClient.GetAsync(requestUrl, cts.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return [];
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var items = JsonSerializer.Deserialize<List<SearchSuggestionItem>>(json, options) ?? [];

                // 4. Sanitize and populate full URLs
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.Image))
                    {
                        item.FullImageUrl = item.Image.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? item.Image
                            : $"{baseHost}/{item.Image.TrimStart('/')}";
                    }

                    if (!string.IsNullOrWhiteSpace(item.Url))
                    {
                        item.FullTargetUrl = item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? item.Url
                            : $"{baseHost}/{item.Url.TrimStart('/')}";
                    }
                    else
                    {
                        var categoryUrl = MapCategoryToSearchUrl(category);
                        item.FullTargetUrl = $"{categoryUrl}?query={Uri.EscapeDataString(item.Label)}";
                    }
                }

                // 5. Store in cache (clean old entries if growing large)
                if (_cache.Count > 100)
                {
                    _cache.Clear();
                }
                _cache[cacheKey] = (DateTime.UtcNow, items);

                return items;
            }
            catch (OperationCanceledException)
            {
                return [];
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to fetch search suggestions for '{Term}'", cleanTerm);
                Debug.WriteLine($"[SearchSuggestionService] Error fetching suggestions: {ex.Message}");
                return [];
            }
        }

        public static string MapCategoryToPart(SearchCategoryType category) => category switch
        {
            SearchCategoryType.Palettes => "color-palettes",
            SearchCategoryType.Colors => "colors",
            SearchCategoryType.Wallpapers => "wallpapers",
            _ => "posts"
        };

        public static string MapCategoryToSearchUrl(SearchCategoryType category) => category switch
        {
            SearchCategoryType.Palettes => AppConstants.Urls.ColorPalettesSearch,
            SearchCategoryType.Colors => AppConstants.Urls.ColorsSearch,
            SearchCategoryType.Wallpapers => AppConstants.Urls.WallpapersSearch,
            _ => AppConstants.Urls.PostsSearch
        };
    }
}
