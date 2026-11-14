using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.Infrastructure.Services;
using Kopolart.Windows.Infrastructure.Settings;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Kopolart.Windows.Services
{
    /// <summary>
    /// Runs the new-post check without opening the main window. Invoked by the Windows Task
    /// Scheduler entry as <c>kopolart-win-x64.exe --rss-check</c> so posts are still detected while
    /// the application stays closed.
    /// </summary>
    public static class HeadlessPostCheckRunner
    {
        public const string CheckArgument = "--rss-check";

        /// <summary>
        /// Returns false when the run should be skipped (the app is already running, notifications
        /// are disabled, nothing is new, or the toast could not be shown).
        /// </summary>
        public static bool Run()
        {
            try
            {
                // When the app is running its own scheduler handles this; skipping also avoids two
                // processes writing the shared settings file at the same time.
                if (IsMainAppRunning())
                {
                    Debug.WriteLine("[HeadlessPostCheck] Main application is running. Skipping scheduled check.");
                    return false;
                }

                // Required before any toast API call in a process without a window.
                TaskbarJumpListManager.EnsureAppUserModelId();

                using var settingsManager = new AppSettingsManager();
                if (!settingsManager.GetNotifyNewContent())
                    return false;

                var rssFeedService = new RssFeedService(settingsManager);
                List<RssFeedItem> unseenPosts = rssFeedService.GetUnseenNewPostsAsync().GetAwaiter().GetResult();
                if (unseenPosts.Count == 0)
                    return false;

                var notifier = new PostNotificationService();
                if (!notifier.ShowDigest(unseenPosts, appIsRunning: false))
                    return false;

                rssFeedService.MarkPostAsNotified(unseenPosts[0]);
                settingsManager.Flush();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HeadlessPostCheck] Scheduled check failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsMainAppRunning()
        {
            try
            {
                using var mutex = Mutex.OpenExisting(Program.AppMutexName);
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
            catch (Exception ex)
            {
                // Access denied and similar cases: assume it is running so we never double-notify.
                Debug.WriteLine($"[HeadlessPostCheck] Mutex probe failed: {ex.Message}");
                return true;
            }
        }
    }
}
