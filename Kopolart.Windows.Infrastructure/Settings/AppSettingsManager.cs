﻿using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Kopolart.Windows.Infrastructure.Settings
{
    public class AppSettingsManager : ISettingsManager, IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly AtomicFileStorage _storage;
        private readonly IAutoStartService _autoStartService;
        private AppSettings _settings;
        private readonly Lock _lock = new();
        private readonly Lock _ioLock = new();
        private readonly Timer _saveTimer;
        private bool _hasPendingChanges;
        private long _saveSequence;
        private long _lastWrittenSequence;

        public AppSettingsManager() : this(null, null)
        {
        }

        public AppSettingsManager(AtomicFileStorage? storage = null, IAutoStartService? autoStartService = null)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.Paths.AppDataFolderName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var settingsFilePath = Path.Combine(folder, "appsettings.json");
            _storage = storage ?? new AtomicFileStorage(settingsFilePath);
            _autoStartService = autoStartService ?? new WindowsAutoStartService();
            _settings = new AppSettings();

            LoadSettings();
            _saveTimer = new Timer(SaveTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void LoadSettings()
        {
            lock (_lock)
            {
                var json = _storage.TryReadContent();
                if (string.IsNullOrWhiteSpace(json))
                    return;

                try
                {
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded == null)
                        return;

                    _settings = loaded;

                    if (_settings.Bookmarks != null)
                    {
                        foreach (var b in _settings.Bookmarks)
                        {
                            b.Url = NormalizeUrl(b.Url);
                            if (string.IsNullOrWhiteSpace(b.Category) || b.Category == "Other")
                            {
                                b.Category = BookmarkItem.DetermineCategory(b.Url);
                            }
                        }
                    }

                    if (_settings.UsageLog != null && _settings.UsageLog.Count > 0)
                    {
                        _settings.UsageSessions ??= new();
                        foreach (var dt in _settings.UsageLog)
                        {
                            _settings.UsageSessions.Add(new UsageSession { Date = dt });
                        }
                        _settings.UsageLog.Clear();
                    }

                    if (_settings.TotalPastUsageMilliseconds == 0 && _settings.TotalPastUsageSeconds > 0)
                    {
                        _settings.TotalPastUsageMilliseconds = (long)(_settings.TotalPastUsageSeconds * 1000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to parse settings JSON: {ex}");
                }
            }
        }

        private void SaveSettings()
        {
            lock (_lock)
            {
                _hasPendingChanges = true;
            }
            _saveTimer.Change(250, Timeout.Infinite);
        }

        private void SaveTimerCallback(object? state)
        {
            SaveSettingsNow();
        }

        private void SaveSettingsNow()
        {
            string json;
            long sequence;

            lock (_lock)
            {
                if (!_hasPendingChanges)
                {
                    return;
                }
                _hasPendingChanges = false;
                json = JsonSerializer.Serialize(_settings, JsonOptions);
                sequence = ++_saveSequence;
            }

            lock (_ioLock)
            {
                if (sequence <= _lastWrittenSequence)
                {
                    return;
                }

                try
                {
                    _storage.WriteAtomic(json);
                    _lastWrittenSequence = sequence;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to save settings: {ex}");
                }
            }
        }

        public void Dispose()
        {
            SaveSettingsNow();
            _saveTimer.Dispose();
        }

        public void ResetToDefaults()
        {
            SetStartMaximized(true);
            SetOpenDashboardAtStartup(true);
            SetShowExitPrompt(true);
            SetShowNavigationLoading(true);
            SetIsDarkMode(false);
            SetAutoStartEnabled(false);
            SetMinimizeToTrayEnabled(false);
            SetLayoutMode("desktop");
            SetShowShortcutClearCachePrompt(true);
            SetRestoreLastVisitedPage(false);
            SetDisableGpuAcceleration(false);
            SetKeepDesktopShortcut(true);
            SetClearCacheOnExit(false);
            SetNotifyNewContent(true);

            lock (_lock)
            {
                _settings.WindowState = 0;
                _settings.WindowLocation = "";
                _settings.WindowSize = "";
                _settings.LastVisitedUrl = "";
            }
            SaveSettings();
        }

        public void Flush()
        {
            SaveSettingsNow();
        }

        public bool GetStartMaximized() { lock (_lock) { return _settings.StartMaximized; } }
        public void SetStartMaximized(bool enabled) { lock (_lock) { _settings.StartMaximized = enabled; } SaveSettings(); }

        public bool GetOpenDashboardAtStartup() { lock (_lock) { return _settings.OpenDashboardAtStartup; } }
        public void SetOpenDashboardAtStartup(bool enabled) { lock (_lock) { _settings.OpenDashboardAtStartup = enabled; } SaveSettings(); }

        public bool GetShowExitPrompt() { lock (_lock) { return _settings.ShowExitPrompt; } }
        public void SetShowExitPrompt(bool enabled) { lock (_lock) { _settings.ShowExitPrompt = enabled; } SaveSettings(); }

        public bool GetShowNavigationLoading() { lock (_lock) { return _settings.ShowNavigationLoading; } }
        public void SetShowNavigationLoading(bool enabled) { lock (_lock) { _settings.ShowNavigationLoading = enabled; } SaveSettings(); }

        public bool GetDesktopShortcutCreated() { lock (_lock) { return _settings.DesktopShortcutCreated; } }
        public void SetDesktopShortcutCreated(bool created) { lock (_lock) { _settings.DesktopShortcutCreated = created; } SaveSettings(); }

        public int GetWindowState() { lock (_lock) { return _settings.WindowState; } }
        public void SetWindowState(int state) { lock (_lock) { _settings.WindowState = state; } SaveSettings(); }

        public string GetWindowLocation() { lock (_lock) { return _settings.WindowLocation ?? ""; } }
        public void SetWindowLocation(string location) { lock (_lock) { _settings.WindowLocation = location; } SaveSettings(); }

        public string GetWindowSize() { lock (_lock) { return _settings.WindowSize ?? ""; } }
        public void SetWindowSize(string size) { lock (_lock) { _settings.WindowSize = size; } SaveSettings(); }

        public bool GetIsDarkMode() { lock (_lock) { return _settings.IsDarkMode; } }
        public void SetIsDarkMode(bool enabled) { lock (_lock) { _settings.IsDarkMode = enabled; } SaveSettings(); }

        public List<BookmarkItem> GetBookmarks()
        {
            lock (_lock)
            {
                return _settings.Bookmarks != null
                    ? new List<BookmarkItem>(_settings.Bookmarks)
                    : new List<BookmarkItem>();
            }
        }

        public void AddBookmark(BookmarkItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Url))
                return;

            item.Url = NormalizeUrl(item.Url);
            if (string.IsNullOrWhiteSpace(item.Category) || item.Category == "Other")
            {
                item.Category = BookmarkItem.DetermineCategory(item.Url);
            }

            lock (_lock)
            {
                _settings.Bookmarks ??= new();
                _settings.Bookmarks.RemoveAll(b => b.Url == item.Url);
                _settings.Bookmarks.Add(item);
            }
            SaveSettings();
        }

        public void RemoveBookmark(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var normalizedUrl = NormalizeUrl(url);
            lock (_lock)
            {
                if (_settings.Bookmarks != null)
                {
                    _settings.Bookmarks.RemoveAll(b => b.Url == normalizedUrl);
                }
            }
            SaveSettings();
        }

        public void UpdateBookmarkTitle(string url, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var normalizedUrl = NormalizeUrl(url);
            lock (_lock)
            {
                if (_settings.Bookmarks != null)
                {
                    var bookmark = _settings.Bookmarks.FirstOrDefault(b => b.Url == normalizedUrl);
                    if (bookmark != null)
                    {
                        bookmark.Title = newTitle;
                    }
                }
            }
            SaveSettings();
        }

        public bool IsBookmarked(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var normalizedUrl = NormalizeUrl(url);
            lock (_lock)
            {
                return _settings.Bookmarks != null && _settings.Bookmarks.Any(b => b.Url == normalizedUrl);
            }
        }

        public void ClearAllBookmarks()
        {
            lock (_lock)
            {
                _settings.Bookmarks?.Clear();
            }
            SaveSettings();
        }

        public List<UsageSession> GetUsageLog()
        {
            lock (_lock)
            {
                return _settings.UsageSessions != null
                    ? new List<UsageSession>(_settings.UsageSessions)
                    : new List<UsageSession>();
            }
        }

        public void LogUsage(DateTime dateTime)
        {
            lock (_lock)
            {
                _settings.UsageSessions ??= new();
                _settings.UsageSessions.Add(new UsageSession { Date = dateTime });
                if (_settings.UsageSessions.Count > 10000)
                {
                    _settings.UsageSessions.RemoveRange(0, _settings.UsageSessions.Count - 10000);
                }
            }
            SaveSettings();
        }

        public void UpdateLastUsageIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            lock (_lock)
            {
                _settings.UsageSessions ??= new();
                if (_settings.UsageSessions.Count > 0)
                {
                    _settings.UsageSessions[^1].Ip = ip;
                }
            }
            SaveSettings();
        }

        public void UpdateLastUsageDuration(long durationSeconds)
        {
            if (durationSeconds < 0) return;
            lock (_lock)
            {
                _settings.UsageSessions ??= new();
                if (_settings.UsageSessions.Count > 0)
                {
                    _settings.UsageSessions[^1].DurationSeconds = durationSeconds;
                }
            }
            SaveSettings();
        }

        public void ClearUsageLog()
        {
            lock (_lock)
            {
                _settings.UsageSessions?.Clear();
            }
            SaveSettings();
        }

        public Dictionary<string, long> GetDailyUsage()
        {
            lock (_lock)
            {
                return _settings.DailyUsageMilliseconds != null
                    ? new Dictionary<string, long>(_settings.DailyUsageMilliseconds, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public long GetDailyUsageForDate(string dateKey)
        {
            if (string.IsNullOrWhiteSpace(dateKey))
                return 0;

            lock (_lock)
            {
                if (_settings.DailyUsageMilliseconds != null &&
                    _settings.DailyUsageMilliseconds.TryGetValue(dateKey, out var ms))
                {
                    return ms;
                }
                return 0;
            }
        }

        public void AddDailyUsage(string dateKey, long milliseconds)
        {
            if (string.IsNullOrWhiteSpace(dateKey) || milliseconds <= 0)
                return;

            lock (_lock)
            {
                _settings.DailyUsageMilliseconds ??= new(StringComparer.OrdinalIgnoreCase);
                if (_settings.DailyUsageMilliseconds.TryGetValue(dateKey, out var current))
                {
                    _settings.DailyUsageMilliseconds[dateKey] = current + milliseconds;
                }
                else
                {
                    _settings.DailyUsageMilliseconds[dateKey] = milliseconds;
                }
            }
            SaveSettings();
        }

        public void SetDailyUsage(Dictionary<string, long> dailyUsage)
        {
            lock (_lock)
            {
                _settings.DailyUsageMilliseconds = dailyUsage != null
                    ? new Dictionary<string, long>(dailyUsage, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            }
            SaveSettings();
        }

        public void ClearDailyUsage()
        {
            lock (_lock)
            {
                _settings.DailyUsageMilliseconds?.Clear();
            }
            SaveSettings();
        }

        public List<DownloadedItem> GetDownloadHistory()
        {
            lock (_lock)
            {
                return _settings.DownloadHistory != null
                    ? new List<DownloadedItem>(_settings.DownloadHistory)
                    : new List<DownloadedItem>();
            }
        }

        public DownloadedItem? GetLastDownloadedItem()
        {
            lock (_lock)
            {
                return _settings.DownloadHistory?.LastOrDefault();
            }
        }

        public void AddDownloadedItem(DownloadedItem item)
        {
            if (item == null)
                return;

            lock (_lock)
            {
                _settings.DownloadHistory ??= new();
                _settings.DownloadHistory.Add(item);
                if (_settings.DownloadHistory.Count > 100)
                {
                    _settings.DownloadHistory.RemoveRange(0, _settings.DownloadHistory.Count - 100);
                }
            }
            SaveSettings();
        }

        public void ClearDownloadHistory()
        {
            lock (_lock)
            {
                _settings.DownloadHistory?.Clear();
            }
            SaveSettings();
        }

        public long GetTotalInternetBytes()
        {
            lock (_lock)
            {
                return _settings.TotalInternetBytes;
            }
        }

        public void SetTotalInternetBytes(long bytes)
        {
            lock (_lock)
            {
                _settings.TotalInternetBytes = bytes;
            }
            SaveSettings();
        }

        public void AddInternetBytes(long bytes)
        {
            if (bytes <= 0) return;
            lock (_lock)
            {
                _settings.TotalInternetBytes += bytes;
            }
            SaveSettings();
        }

        public Dictionary<string, long> GetDailyInternetBytes()
        {
            lock (_lock)
            {
                return _settings.DailyInternetBytes != null
                    ? new Dictionary<string, long>(_settings.DailyInternetBytes, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public long GetDailyInternetBytes(string dateKey)
        {
            if (string.IsNullOrWhiteSpace(dateKey)) return 0;
            lock (_lock)
            {
                if (_settings.DailyInternetBytes != null &&
                    _settings.DailyInternetBytes.TryGetValue(dateKey, out var bytes))
                {
                    return bytes;
                }
                return 0;
            }
        }

        public void AddDailyInternetBytes(string dateKey, long bytes)
        {
            if (string.IsNullOrWhiteSpace(dateKey) || bytes <= 0) return;
            lock (_lock)
            {
                _settings.DailyInternetBytes ??= new(StringComparer.OrdinalIgnoreCase);
                if (_settings.DailyInternetBytes.TryGetValue(dateKey, out var current))
                {
                    _settings.DailyInternetBytes[dateKey] = current + bytes;
                }
                else
                {
                    _settings.DailyInternetBytes[dateKey] = bytes;
                }
            }
            SaveSettings();
        }

        public void ClearInternetBytes()
        {
            lock (_lock)
            {
                _settings.TotalInternetBytes = 0;
                _settings.DailyInternetBytes?.Clear();
            }
            SaveSettings();
        }

        public double GetTotalPastUsageSeconds() => GetTotalPastUsageMilliseconds() / 1000.0;
        public void SetTotalPastUsageSeconds(double seconds) => SetTotalPastUsageMilliseconds((long)(seconds * 1000));

        public long GetTotalPastUsageMilliseconds()
        {
            lock (_lock)
            {
                return _settings.TotalPastUsageMilliseconds;
            }
        }

        public void SetTotalPastUsageMilliseconds(long ms)
        {
            lock (_lock)
            {
                _settings.TotalPastUsageMilliseconds = ms;
            }
            SaveSettings();
        }

        public List<string> GetAchievedMilestones()
        {
            lock (_lock)
            {
                return _settings.AchievedMilestones != null
                    ? new List<string>(_settings.AchievedMilestones)
                    : new List<string>();
            }
        }

        public void MarkMilestoneAchieved(string milestone)
        {
            lock (_lock)
            {
                _settings.AchievedMilestones ??= new();
                if (!_settings.AchievedMilestones.Contains(milestone))
                {
                    _settings.AchievedMilestones.Add(milestone);
                }
            }
            SaveSettings();
        }

        public void ClearAchievedMilestones()
        {
            lock (_lock)
            {
                _settings.AchievedMilestones?.Clear();
            }
            SaveSettings();
        }

        public DateTime? GetLastClosedTime()
        {
            lock (_lock)
            {
                return _settings.LastClosedTime;
            }
        }

        public void SetLastClosedTime(DateTime? dateTime)
        {
            lock (_lock)
            {
                _settings.LastClosedTime = dateTime;
            }
            SaveSettings();
        }

        public bool GetAutoStartEnabled() { lock (_lock) { return _settings.AutoStartEnabled; } }
        public void SetAutoStartEnabled(bool enabled)
        {
            lock (_lock)
            {
                _settings.AutoStartEnabled = enabled;
            }
            SaveSettings();
            _autoStartService.SetAutoStart(enabled);
        }

        public bool GetMinimizeToTrayEnabled() { lock (_lock) { return _settings.MinimizeToTrayEnabled; } }
        public void SetMinimizeToTrayEnabled(bool enabled) { lock (_lock) { _settings.MinimizeToTrayEnabled = enabled; } SaveSettings(); }

        public string GetLayoutMode() { lock (_lock) { return _settings.LayoutMode ?? "desktop"; } }
        public void SetLayoutMode(string mode) { lock (_lock) { _settings.LayoutMode = mode; } SaveSettings(); }
        public bool GetShowShortcutClearCachePrompt() { lock (_lock) { return _settings.ShowShortcutClearCachePrompt; } }
        public void SetShowShortcutClearCachePrompt(bool enabled) { lock (_lock) { _settings.ShowShortcutClearCachePrompt = enabled; } SaveSettings(); }
        public DateTime? GetLastUpdatePromptTime() { lock (_lock) { return _settings.LastUpdatePromptTime; } }
        public void SetLastUpdatePromptTime(DateTime? dateTime) { lock (_lock) { _settings.LastUpdatePromptTime = dateTime; } SaveSettings(); }
        public DateTime? GetLastUpdateCheckTime() { lock (_lock) { return _settings.LastUpdateCheckTime; } }
        public void SetLastUpdateCheckTime(DateTime? dateTime) { lock (_lock) { _settings.LastUpdateCheckTime = dateTime; } SaveSettings(); }

        public List<VisitedPage> GetHistory()
        {
            lock (_lock)
            {
                return _settings.NavigationHistory != null
                    ? new List<VisitedPage>(_settings.NavigationHistory)
                    : new List<VisitedPage>();
            }
        }

        public int GetHistoryCount()
        {
            lock (_lock)
            {
                return _settings.NavigationHistory?.Count ?? 0;
            }
        }

        public void AddToHistory(string title, string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url == "about:blank" || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return;

            var normalizedUrl = NormalizeUrl(url);
            lock (_lock)
            {
                _settings.NavigationHistory ??= new();
                _settings.NavigationHistory.RemoveAll(h => h.Url == normalizedUrl);
                _settings.NavigationHistory.Insert(0, new VisitedPage
                {
                    Title = string.IsNullOrWhiteSpace(title) ? normalizedUrl : title,
                    Url = normalizedUrl,
                    VisitedAt = DateTime.Now,
                    Category = ContentCategoryHelper.DetermineCategoryString(normalizedUrl)
                });

                if (_settings.NavigationHistory.Count > 50)
                {
                    _settings.NavigationHistory.RemoveRange(50, _settings.NavigationHistory.Count - 50);
                }
            }
            SaveSettings();
        }

        public void ClearHistory()
        {
            lock (_lock)
            {
                _settings.NavigationHistory?.Clear();
            }
            SaveSettings();
        }

        public string GetLastVisitedUrl() { lock (_lock) { return _settings.LastVisitedUrl ?? ""; } }
        public void SetLastVisitedUrl(string url) { lock (_lock) { _settings.LastVisitedUrl = url; } SaveSettings(); }

        public bool GetRestoreLastVisitedPage() { lock (_lock) { return _settings.RestoreLastVisitedPage; } }
        public void SetRestoreLastVisitedPage(bool enabled) { lock (_lock) { _settings.RestoreLastVisitedPage = enabled; } SaveSettings(); }

        public string GetLastAppVersion() { lock (_lock) { return _settings.LastAppVersion ?? ""; } }
        public void SetLastAppVersion(string version) { lock (_lock) { _settings.LastAppVersion = version ?? ""; } SaveSettings(); }

        public bool GetDisableGpuAcceleration() { lock (_lock) { return _settings.DisableGpuAcceleration; } }
        public void SetDisableGpuAcceleration(bool enabled) { lock (_lock) { _settings.DisableGpuAcceleration = enabled; } SaveSettings(); }

        public bool GetKeepDesktopShortcut() { lock (_lock) { return _settings.KeepDesktopShortcut; } }
        public void SetKeepDesktopShortcut(bool enabled) { lock (_lock) { _settings.KeepDesktopShortcut = enabled; } SaveSettings(); }

        public bool GetClearCacheOnExit() { lock (_lock) { return _settings.ClearCacheOnExit; } }
        public void SetClearCacheOnExit(bool enabled) { lock (_lock) { _settings.ClearCacheOnExit = enabled; } SaveSettings(); }

        public bool GetNotifyNewContent() { lock (_lock) { return _settings.NotifyNewContent; } }
        public void SetNotifyNewContent(bool enabled) { lock (_lock) { _settings.NotifyNewContent = enabled; } SaveSettings(); }

        public string GetLastNotifiedPostGuid() { lock (_lock) { return _settings.LastNotifiedPostGuid ?? ""; } }
        public void SetLastNotifiedPostGuid(string guid) { lock (_lock) { _settings.LastNotifiedPostGuid = guid ?? ""; } SaveSettings(); }

        public DateTime? GetLastNotifiedPostUtc() { lock (_lock) { return _settings.LastNotifiedPostUtc; } }
        public void SetLastNotifiedPostUtc(DateTime? utc) { lock (_lock) { _settings.LastNotifiedPostUtc = utc; } SaveSettings(); }

        public bool GetRssBaselineInitialized() { lock (_lock) { return _settings.RssBaselineInitialized; } }
        public void SetRssBaselineInitialized(bool initialized) { lock (_lock) { _settings.RssBaselineInitialized = initialized; } SaveSettings(); }

        public string GetLastSelectedSearchCategory() { lock (_lock) { return _settings.LastSelectedSearchCategory ?? "Posts"; } }
        public void SetLastSelectedSearchCategory(string category) { lock (_lock) { _settings.LastSelectedSearchCategory = category ?? "Posts"; } SaveSettings(); }

        public List<string> GetRecentSearches()
        {
            lock (_lock)
            {
                return _settings.RecentSearchQueries != null
                    ? new List<string>(_settings.RecentSearchQueries)
                    : new List<string>();
            }
        }

        public void AddRecentSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            lock (_lock)
            {
                _settings.RecentSearchQueries ??= new List<string>();
                query = query.Trim();
                _settings.RecentSearchQueries.RemoveAll(q => string.Equals(q, query, StringComparison.OrdinalIgnoreCase));
                _settings.RecentSearchQueries.Insert(0, query);
                if (_settings.RecentSearchQueries.Count > 5)
                {
                    _settings.RecentSearchQueries = _settings.RecentSearchQueries.Take(5).ToList();
                }
            }
            SaveSettings();
            Flush();
        }

        public void RemoveRecentSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            lock (_lock)
            {
                _settings.RecentSearchQueries ??= new List<string>();
                query = query.Trim();
                _settings.RecentSearchQueries.RemoveAll(q => string.Equals(q, query, StringComparison.OrdinalIgnoreCase));
            }
            SaveSettings();
            Flush();
        }

        public void ClearRecentSearches()
        {
            lock (_lock)
            {
                _settings.RecentSearchQueries?.Clear();
            }
            SaveSettings();
            Flush();
        }

        public static string NormalizeUrl(string url) => UrlNormalizer.NormalizeUrl(url);

        public AppSettings GetSettingsSnapshot()
        {
            lock (_lock)
            {
                return SettingsDataMerger.CreateExportSnapshot(_settings);
            }
        }

        public string CreateSafetyBackup()
        {
            lock (_ioLock)
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.Paths.AppDataFolderName, "Backups");
                return _storage.CreateSafetyBackup(folder);
            }
        }

        public void ApplySettingsSnapshot(AppSettings snapshot, DataImportMode mode)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            lock (_lock)
            {
                _settings = SettingsDataMerger.ApplyImport(_settings, snapshot, mode);
                _hasPendingChanges = true;
            }

            SaveSettingsNow();
        }
    }
}
