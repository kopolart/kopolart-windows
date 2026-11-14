using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Kopolart.Windows.Infrastructure.Settings
{
    public static class SettingsDataMerger
    {
        public static AppSettings CreateExportSnapshot(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            var clone = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            // Omit machine-specific window coordinates from export
            clone.WindowState = 0;
            clone.WindowLocation = "";
            clone.WindowSize = "";
            return clone;
        }

        public static AppSettings ApplyImport(AppSettings current, AppSettings incoming, DataImportMode mode)
        {
            if (incoming == null)
                throw new ArgumentNullException(nameof(incoming));

            var currentWindowState = current.WindowState;
            var currentWindowLocation = current.WindowLocation;
            var currentWindowSize = current.WindowSize;
            var currentDesktopShortcut = current.DesktopShortcutCreated;

            if (mode == DataImportMode.ReplaceAll)
            {
                var target = incoming;
                // Preserve machine-local window layout
                target.WindowState = currentWindowState;
                target.WindowLocation = currentWindowLocation;
                target.WindowSize = currentWindowSize;
                target.DesktopShortcutCreated = currentDesktopShortcut;

                if (target.Bookmarks != null)
                {
                    foreach (var b in target.Bookmarks)
                    {
                        b.Url = UrlNormalizer.NormalizeUrl(b.Url);
                        if (string.IsNullOrWhiteSpace(b.Category) || b.Category == "Other")
                        {
                            b.Category = BookmarkItem.DetermineCategory(b.Url);
                        }
                    }
                }

                return target;
            }

            // Merge mode
            current.IsDarkMode = incoming.IsDarkMode;
            current.StartMaximized = incoming.StartMaximized;
            current.OpenDashboardAtStartup = incoming.OpenDashboardAtStartup;
            current.ShowExitPrompt = incoming.ShowExitPrompt;
            current.ShowNavigationLoading = incoming.ShowNavigationLoading;
            current.AutoStartEnabled = incoming.AutoStartEnabled;
            current.MinimizeToTrayEnabled = incoming.MinimizeToTrayEnabled;
            current.LayoutMode = incoming.LayoutMode ?? current.LayoutMode;
            current.RestoreLastVisitedPage = incoming.RestoreLastVisitedPage;
            current.DisableGpuAcceleration = incoming.DisableGpuAcceleration;
            current.KeepDesktopShortcut = incoming.KeepDesktopShortcut;
            current.ClearCacheOnExit = incoming.ClearCacheOnExit;
            current.ShowShortcutClearCachePrompt = incoming.ShowShortcutClearCachePrompt;
            current.NotifyNewContent = incoming.NotifyNewContent;
            if (!string.IsNullOrWhiteSpace(incoming.LastSelectedSearchCategory))
                current.LastSelectedSearchCategory = incoming.LastSelectedSearchCategory;

            if (incoming.RecentSearchQueries != null)
            {
                current.RecentSearchQueries ??= new();
                foreach (var q in incoming.RecentSearchQueries)
                {
                    if (!string.IsNullOrWhiteSpace(q) && !current.RecentSearchQueries.Any(x => string.Equals(x, q, StringComparison.OrdinalIgnoreCase)))
                    {
                        current.RecentSearchQueries.Add(q);
                    }
                }
                if (current.RecentSearchQueries.Count > 5)
                {
                    current.RecentSearchQueries = current.RecentSearchQueries.Take(5).ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(incoming.LastVisitedUrl))
                current.LastVisitedUrl = incoming.LastVisitedUrl;

            // Merge Bookmarks
            current.Bookmarks ??= new();
            if (incoming.Bookmarks != null)
            {
                foreach (var b in incoming.Bookmarks)
                {
                    var normUrl = UrlNormalizer.NormalizeUrl(b.Url);
                    if (!string.IsNullOrWhiteSpace(normUrl) && !current.Bookmarks.Exists(x => x.Url == normUrl))
                    {
                        b.Url = normUrl;
                        if (string.IsNullOrWhiteSpace(b.Category) || b.Category == "Other")
                            b.Category = BookmarkItem.DetermineCategory(normUrl);
                        current.Bookmarks.Add(b);
                    }
                }
            }

            // Merge Navigation History
            current.NavigationHistory ??= new();
            if (incoming.NavigationHistory != null)
            {
                foreach (var h in incoming.NavigationHistory)
                {
                    var normUrl = UrlNormalizer.NormalizeUrl(h.Url);
                    if (!string.IsNullOrWhiteSpace(normUrl) && !current.NavigationHistory.Exists(x => x.Url == normUrl))
                    {
                        h.Url = normUrl;
                        current.NavigationHistory.Add(h);
                    }
                }
                current.NavigationHistory.Sort((a, b) => b.VisitedAt.CompareTo(a.VisitedAt));
                if (current.NavigationHistory.Count > 50)
                {
                    current.NavigationHistory.RemoveRange(50, current.NavigationHistory.Count - 50);
                }
            }

            // Merge Download History
            current.DownloadHistory ??= new();
            if (incoming.DownloadHistory != null)
            {
                foreach (var d in incoming.DownloadHistory)
                {
                    if (!current.DownloadHistory.Exists(x => x.Url == d.Url && x.FileName == d.FileName))
                    {
                        current.DownloadHistory.Add(d);
                    }
                }
                current.DownloadHistory.Sort((a, b) => b.DownloadedAt.CompareTo(a.DownloadedAt));
                if (current.DownloadHistory.Count > 100)
                {
                    current.DownloadHistory.RemoveRange(100, current.DownloadHistory.Count - 100);
                }
            }

            // Merge Usage Sessions
            current.UsageSessions ??= new();
            if (incoming.UsageSessions != null)
            {
                var existingDates = new HashSet<DateTime>(current.UsageSessions.Select(s => s.Date));
                foreach (var s in incoming.UsageSessions)
                {
                    if (existingDates.Add(s.Date))
                    {
                        current.UsageSessions.Add(s);
                    }
                }
                current.UsageSessions.Sort((a, b) => a.Date.CompareTo(b.Date));
                if (current.UsageSessions.Count > 10000)
                {
                    current.UsageSessions.RemoveRange(0, current.UsageSessions.Count - 10000);
                }
            }

            // Merge Daily Usage Milliseconds
            current.DailyUsageMilliseconds ??= new(StringComparer.OrdinalIgnoreCase);
            if (incoming.DailyUsageMilliseconds != null)
            {
                foreach (var kvp in incoming.DailyUsageMilliseconds)
                {
                    if (current.DailyUsageMilliseconds.TryGetValue(kvp.Key, out var currentVal))
                    {
                        current.DailyUsageMilliseconds[kvp.Key] = Math.Max(currentVal, kvp.Value);
                    }
                    else
                    {
                        current.DailyUsageMilliseconds[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Merge Daily Internet Bytes
            current.DailyInternetBytes ??= new(StringComparer.OrdinalIgnoreCase);
            if (incoming.DailyInternetBytes != null)
            {
                foreach (var kvp in incoming.DailyInternetBytes)
                {
                    if (current.DailyInternetBytes.TryGetValue(kvp.Key, out var currentVal))
                    {
                        current.DailyInternetBytes[kvp.Key] = Math.Max(currentVal, kvp.Value);
                    }
                    else
                    {
                        current.DailyInternetBytes[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Total Usage and Data
            current.TotalPastUsageMilliseconds = Math.Max(current.TotalPastUsageMilliseconds, incoming.TotalPastUsageMilliseconds);
            current.TotalInternetBytes = Math.Max(current.TotalInternetBytes, incoming.TotalInternetBytes);

            // Merge Milestones
            current.AchievedMilestones ??= new();
            if (incoming.AchievedMilestones != null)
            {
                foreach (var m in incoming.AchievedMilestones)
                {
                    if (!current.AchievedMilestones.Contains(m))
                    {
                        current.AchievedMilestones.Add(m);
                    }
                }
            }

            return current;
        }
    }
}
