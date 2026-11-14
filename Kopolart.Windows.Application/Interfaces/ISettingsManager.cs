using Kopolart.Windows.Domain.Models;
using System;
using System.Collections.Generic;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface ISettingsManager
    {
        bool GetStartMaximized();
        void SetStartMaximized(bool enabled);

        bool GetOpenDashboardAtStartup();
        void SetOpenDashboardAtStartup(bool enabled);

        bool GetShowExitPrompt();
        void SetShowExitPrompt(bool enabled);

        bool GetShowNavigationLoading();
        void SetShowNavigationLoading(bool enabled);

        bool GetDesktopShortcutCreated();
        void SetDesktopShortcutCreated(bool created);

        int GetWindowState();
        void SetWindowState(int state);

        string GetWindowLocation();
        void SetWindowLocation(string location);

        string GetWindowSize();
        void SetWindowSize(string size);

        bool GetIsDarkMode();
        void SetIsDarkMode(bool enabled);

        List<BookmarkItem> GetBookmarks();
        void AddBookmark(BookmarkItem item);
        void RemoveBookmark(string url);
        bool IsBookmarked(string url);
        void ClearAllBookmarks();

        List<UsageSession> GetUsageLog();
        void LogUsage(DateTime dateTime);
        void UpdateLastUsageIp(string ip);
        void UpdateLastUsageDuration(long durationSeconds);
        void ClearUsageLog();

        Dictionary<string, long> GetDailyUsage();
        long GetDailyUsageForDate(string dateKey);
        void AddDailyUsage(string dateKey, long milliseconds);
        void SetDailyUsage(Dictionary<string, long> dailyUsage);
        void ClearDailyUsage();

        List<DownloadedItem> GetDownloadHistory();
        DownloadedItem? GetLastDownloadedItem();
        void AddDownloadedItem(DownloadedItem item);
        void ClearDownloadHistory();

        long GetTotalInternetBytes();
        void SetTotalInternetBytes(long bytes);
        void AddInternetBytes(long bytes);
        Dictionary<string, long> GetDailyInternetBytes();
        void AddDailyInternetBytes(string dateKey, long bytes);
        void ClearInternetBytes();

        double GetTotalPastUsageSeconds();
        void SetTotalPastUsageSeconds(double seconds);

        long GetTotalPastUsageMilliseconds();
        void SetTotalPastUsageMilliseconds(long ms);

        List<string> GetAchievedMilestones();
        void MarkMilestoneAchieved(string milestone);
        void ClearAchievedMilestones();

        DateTime? GetLastClosedTime();
        void SetLastClosedTime(DateTime? dateTime);

        bool GetAutoStartEnabled();
        void SetAutoStartEnabled(bool enabled);

        bool GetMinimizeToTrayEnabled();
        void SetMinimizeToTrayEnabled(bool enabled);

        string GetLayoutMode();
        void SetLayoutMode(string mode);

        bool GetShowShortcutClearCachePrompt();
        void SetShowShortcutClearCachePrompt(bool enabled);

        DateTime? GetLastUpdatePromptTime();
        void SetLastUpdatePromptTime(DateTime? dateTime);

        DateTime? GetLastUpdateCheckTime();
        void SetLastUpdateCheckTime(DateTime? dateTime);

        List<VisitedPage> GetHistory();
        int GetHistoryCount();
        void AddToHistory(string title, string url);
        void ClearHistory();

        string GetLastVisitedUrl();
        void SetLastVisitedUrl(string url);

        bool GetRestoreLastVisitedPage();
        void SetRestoreLastVisitedPage(bool enabled);

        string GetLastAppVersion();
        void SetLastAppVersion(string version);

        bool GetDisableGpuAcceleration();
        void SetDisableGpuAcceleration(bool enabled);

        bool GetKeepDesktopShortcut();
        void SetKeepDesktopShortcut(bool enabled);

        bool GetClearCacheOnExit();
        void SetClearCacheOnExit(bool enabled);

        bool GetNotifyNewContent();
        void SetNotifyNewContent(bool enabled);
        string GetLastNotifiedPostGuid();
        void SetLastNotifiedPostGuid(string guid);
        DateTime? GetLastNotifiedPostUtc();
        void SetLastNotifiedPostUtc(DateTime? utc);
        bool GetRssBaselineInitialized();
        void SetRssBaselineInitialized(bool initialized);

        string GetLastSelectedSearchCategory();
        void SetLastSelectedSearchCategory(string category);

        List<string> GetRecentSearches();
        void AddRecentSearch(string query);
        void RemoveRecentSearch(string query);
        void ClearRecentSearches();

        void UpdateBookmarkTitle(string url, string newTitle);

        void ResetToDefaults();
        void Flush();

        AppSettings GetSettingsSnapshot();
        void ApplySettingsSnapshot(AppSettings snapshot, DataImportMode mode);
        string CreateSafetyBackup();
    }
}
