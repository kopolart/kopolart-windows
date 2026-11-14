using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class RssFeedService : IRssFeedService
    {
        /// <summary>Upper bound of posts summarised in one digest notification.</summary>
        public const int MaxDigestItems = 10;

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(25);
        private readonly ISettingsManager _settingsManager;
        private readonly IHttpClientFactory? _httpClientFactory;

        private static readonly object _cacheLock = new();
        private static List<RssFeedItem>? _cachedFeed;
        private static DateTime _cacheTimeUtc = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public RssFeedService(ISettingsManager settingsManager, IHttpClientFactory? httpClientFactory = null)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<RssFeedItem>> FetchFeedAsync(CancellationToken cancellationToken = default)
        {
            lock (_cacheLock)
            {
                if (_cachedFeed != null && (DateTime.UtcNow - _cacheTimeUtc) < CacheDuration)
                {
                    return new List<RssFeedItem>(_cachedFeed);
                }
            }

            var result = new List<RssFeedItem>();

            // Fast offline check - if offline, return cached feed or empty list immediately without waiting
            try
            {
                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    lock (_cacheLock)
                    {
                        return _cachedFeed != null ? new List<RssFeedItem>(_cachedFeed) : result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RssFeedService] NetworkInterface check warning: {ex.Message}");
            }
            try
            {
                HttpClient client;
                bool shouldDispose = false;

                if (_httpClientFactory != null)
                {
                    client = _httpClientFactory.CreateClient("DefaultClient");
                }
                else
                {
                    var handler = new HttpClientHandler
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                    };
                    client = new HttpClient(handler) { Timeout = DefaultTimeout };
                    shouldDispose = true;
                }

                try
                {
                    var xmlString = await client.GetStringAsync(AppConstants.Urls.RssFeed, cancellationToken);
                    if (string.IsNullOrWhiteSpace(xmlString))
                        return result;

                    var doc = XDocument.Parse(xmlString);
                    var items = doc.Descendants("item");

                    XNamespace mediaNs = "http://search.yahoo.com/mrss/";

                    foreach (var item in items)
                    {
                        var title = item.Element("title")?.Value?.Trim() ?? string.Empty;
                        var link = item.Element("link")?.Value?.Trim() ?? string.Empty;
                        var desc = item.Element("description")?.Value?.Trim() ?? string.Empty;
                        var guid = item.Element("guid")?.Value?.Trim();
                        var category = item.Element("category")?.Value?.Trim();
                        var pubDateStr = item.Element("pubDate")?.Value?.Trim();

                        DateTime? pubDate = null;
                        if (!string.IsNullOrWhiteSpace(pubDateStr) &&
                            DateTime.TryParse(pubDateStr, System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var parsedDate))
                        {
                            pubDate = parsedDate;
                        }

                        string? imageUrl = item.Element(mediaNs + "content")?.Attribute("url")?.Value?.Trim()
                            ?? item.Element("enclosure")?.Attribute("url")?.Value?.Trim();

                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(link))
                        {
                            result.Add(new RssFeedItem
                            {
                                Title = title,
                                Link = link,
                                Description = desc,
                                PublishDate = pubDate,
                                ImageUrl = imageUrl,
                                Category = category,
                                Guid = !string.IsNullOrWhiteSpace(guid) ? guid : link
                            });
                        }
                    }

                    if (result.Count > 0)
                    {
                        lock (_cacheLock)
                        {
                            _cachedFeed = new List<RssFeedItem>(result);
                            _cacheTimeUtc = DateTime.UtcNow;
                        }
                    }
                }
                finally
                {
                    if (shouldDispose)
                    {
                        client.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RssFeedService] Error fetching RSS feed: {ex.Message}");
            }

            return result;
        }

        public async Task<RssFeedItem?> GetLatestNewPostAsync(CancellationToken cancellationToken = default)
        {
            var posts = await GetUnseenNewPostsAsync(cancellationToken);
            return posts.Count > 0 ? posts[0] : null;
        }

        /// <summary>
        /// Returns every feed item published after the last announced post (newest first), so a
        /// single digest notification can report all of them instead of only the top item.
        /// </summary>
        public async Task<List<RssFeedItem>> GetUnseenNewPostsAsync(CancellationToken cancellationToken = default)
        {
            var unseen = new List<RssFeedItem>();

            if (!CanNotifyNow())
                return unseen;

            var items = await FetchFeedAsync(cancellationToken);
            if (items.Count == 0)
                return unseen;

            // Rule: On the first time this feature runs, record the current latest post as baseline
            // so existing posts are NOT spammed to the user as "new"!
            bool baselineInit = _settingsManager.GetRssBaselineInitialized();
            string lastNotifiedId = _settingsManager.GetLastNotifiedPostGuid();

            if (!baselineInit || string.IsNullOrWhiteSpace(lastNotifiedId))
            {
                _settingsManager.SetLastNotifiedPostGuid(GetItemId(items[0]));
                _settingsManager.SetRssBaselineInitialized(true);
                return unseen;
            }

            int limit = Math.Max(1, Math.Min(MaxDigestItems, items.Count));
            bool reachedAnnouncedPost = false;

            foreach (var item in items)
            {
                // Deduplication rule: stop at the last post we already announced to the user
                if (IsSamePost(item, lastNotifiedId))
                {
                    reachedAnnouncedPost = true;
                    break;
                }

                unseen.Add(item);
                if (unseen.Count >= limit)
                    break;
            }

            // The stored post is no longer present in the feed (feed rotated or republished):
            // report only the newest post instead of dumping the whole feed as "new".
            if (!reachedAnnouncedPost && unseen.Count >= items.Count)
            {
                return new List<RssFeedItem> { items[0] };
            }

            return unseen;
        }

        private static string GetItemId(RssFeedItem item)
        {
            return !string.IsNullOrWhiteSpace(item.Guid) ? item.Guid : item.Link;
        }

        private static bool IsSamePost(RssFeedItem item, string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                return false;

            return string.Equals(postId, GetItemId(item), StringComparison.OrdinalIgnoreCase)
                || string.Equals(postId, item.Link, StringComparison.OrdinalIgnoreCase)
                || string.Equals(postId, item.Guid, StringComparison.OrdinalIgnoreCase);
        }

        public void MarkPostAsNotified(RssFeedItem post)
        {
            if (post == null) return;
            string id = !string.IsNullOrWhiteSpace(post.Guid) ? post.Guid : post.Link;
            _settingsManager.SetLastNotifiedPostGuid(id);
            _settingsManager.SetLastNotifiedPostUtc(DateTime.UtcNow);
        }

        public bool CanNotifyNow()
        {
            if (!_settingsManager.GetNotifyNewContent())
                return false;

            var lastNotifiedUtc = _settingsManager.GetLastNotifiedPostUtc();
            if (lastNotifiedUtc.HasValue)
            {
                // Strict rule: at most one notification within 3 days
                if ((DateTime.UtcNow - lastNotifiedUtc.Value).TotalDays < 3.0)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<Dictionary<string, int>> GetRecentCategoryCountsAsync(TimeSpan? window = null, CancellationToken cancellationToken = default)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["colors"] = 0,
                ["color-palettes"] = 0,
                ["posts"] = 0,
                ["wallpapers"] = 0
            };

            var maxAge = window ?? TimeSpan.FromDays(7);
            var items = await FetchFeedAsync(cancellationToken);
            var nowUtc = DateTime.UtcNow;

            foreach (var item in items)
            {
                if (!item.PublishDate.HasValue) continue;
                var pubUtc = item.PublishDate.Value.ToUniversalTime();
                var age = nowUtc - pubUtc;
                if (age.TotalSeconds >= -300 && age <= maxAge)
                {
                    var catKey = MatchCategoryKey(item.Link);
                    if (catKey != null && counts.ContainsKey(catKey))
                    {
                        counts[catKey]++;
                    }
                }
            }

            return counts;
        }

        public static string? MatchCategoryKey(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (url.Contains("/color-palettes", StringComparison.OrdinalIgnoreCase))
                return "color-palettes";

            if (url.Contains("/colors", StringComparison.OrdinalIgnoreCase))
                return "colors";

            if (url.Contains("/posts", StringComparison.OrdinalIgnoreCase))
                return "posts";

            if (url.Contains("/wallpapers", StringComparison.OrdinalIgnoreCase))
                return "wallpapers";

            return null;
        }
    }
}
