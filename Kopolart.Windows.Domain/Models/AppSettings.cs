using System;
using System.Collections.Generic;

namespace Kopolart.Windows.Domain.Models
{
    public class AppSettings
    {
        public bool StartMaximized { get; set; } = true;
        public bool OpenDashboardAtStartup { get; set; } = true;
        public bool ShowExitPrompt { get; set; } = true;
        public bool ShowNavigationLoading { get; set; } = true;
        public bool DesktopShortcutCreated { get; set; } = false;
        public int WindowState { get; set; } = 0; // Normal
        public string WindowLocation { get; set; } = "";
        public string WindowSize { get; set; } = "";
        public bool IsDarkMode { get; set; } = false;
        public List<BookmarkItem> Bookmarks { get; set; } = new();

        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<DateTime>? UsageLog { get; set; } = null;

        public List<UsageSession> UsageSessions { get; set; } = new();
        public Dictionary<string, long> DailyUsageMilliseconds { get; set; } = new();
        public List<DownloadedItem> DownloadHistory { get; set; } = new();
        public long TotalInternetBytes { get; set; } = 0;
        public Dictionary<string, long> DailyInternetBytes { get; set; } = new();

        /// <summary>
        /// Deprecated legacy property; TotalPastUsageMilliseconds is the canonical storage.
        /// Ignored when saving to prevent duplicate fields in JSON.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Always)]
        public double TotalPastUsageSeconds { get; set; } = 0;

        public long TotalPastUsageMilliseconds { get; set; } = 0;
        public List<string> AchievedMilestones { get; set; } = new();
        public DateTime? LastClosedTime { get; set; } = null;
        public bool AutoStartEnabled { get; set; } = false;
        public bool MinimizeToTrayEnabled { get; set; } = false;
        public string LayoutMode { get; set; } = "desktop";
        public bool ShowShortcutClearCachePrompt { get; set; } = true;
        public DateTime? LastUpdatePromptTime { get; set; } = null;
        public DateTime? LastUpdateCheckTime { get; set; } = null;
        public List<VisitedPage> NavigationHistory { get; set; } = new();
        public string LastVisitedUrl { get; set; } = "";
        public bool RestoreLastVisitedPage { get; set; } = false;
        public string LastAppVersion { get; set; } = "";
        public bool DisableGpuAcceleration { get; set; } = false;
        public bool KeepDesktopShortcut { get; set; } = true;
        public bool ClearCacheOnExit { get; set; } = false;
        public bool NotifyNewContent { get; set; } = true;
        public string LastNotifiedPostGuid { get; set; } = "";
        public DateTime? LastNotifiedPostUtc { get; set; } = null;
        public bool RssBaselineInitialized { get; set; } = false;
        public string LastSelectedSearchCategory { get; set; } = "Posts";
        public List<string> RecentSearchQueries { get; set; } = new();
    }
}
