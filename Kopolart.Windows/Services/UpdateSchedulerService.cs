using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.Services
{
    /// <summary>
    /// Background scheduler service that checks for application updates every 48 hours
    /// and triggers notifications to the user when a newer version is available.
    /// Completely decouples update polling and scheduling from UI forms.
    /// </summary>
    public class UpdateSchedulerService(
        IUpdateService updateService,
        ISettingsManager settingsManager,
        IDownloadService downloadService,
        IUpdatePackageVerifier packageVerifier,
        IRssFeedService? rssFeedService = null)
        : IDisposable
    {
        private const double CheckIntervalHours = 48.0;
        private readonly IUpdateService _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        private readonly ISettingsManager _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        private readonly IDownloadService _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        private readonly IUpdatePackageVerifier _packageVerifier = packageVerifier ?? throw new ArgumentNullException(nameof(packageVerifier));
        private readonly IRssFeedService? _rssFeedService = rssFeedService;

        private System.Windows.Forms.Timer? _periodicTimer;
        private Form? _parentForm;
        private Action<bool>? _setUpdateBadgeAction;
        private Action<UpdateInfo>? _showNotificationAction;
        private Action<IReadOnlyList<RssFeedItem>>? _showNewContentNotificationAction;
        private Func<bool>? _isDarkModeProvider;
        private int _isChecking = 0;

        /// <summary>
        /// Starts the background update scheduler.
        /// </summary>
        public void Start(
            Form parentForm,
            Action<bool> setUpdateBadgeAction,
            Action<UpdateInfo> showNotificationAction,
            Func<bool> isDarkModeProvider,
            Action<IReadOnlyList<RssFeedItem>>? showNewContentNotificationAction = null)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _setUpdateBadgeAction = setUpdateBadgeAction;
            _showNotificationAction = showNotificationAction;
            _isDarkModeProvider = isDarkModeProvider;
            _showNewContentNotificationAction = showNewContentNotificationAction;

            Stop();

            // Run initial check after a graceful 5-second startup delay
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000);
                    if (_parentForm != null && !_parentForm.IsDisposed)
                    {
                        await CheckForUpdatesAsync(force: false);
                        await CheckForNewContentAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UpdateSchedulerService] Initial delay error: {ex.Message}");
                }
            });

            // Periodic timer checks every 1 hour whether interval has elapsed
            _periodicTimer = new System.Windows.Forms.Timer
            {
                Interval = (int)TimeSpan.FromHours(1).TotalMilliseconds
            };
            _periodicTimer.Tick += PeriodicTimer_Tick;
            _periodicTimer.Start();
        }

        private async void PeriodicTimer_Tick(object? sender, EventArgs e)
        {
            await CheckForUpdatesAsync(force: false);
            await CheckForNewContentAsync();
        }

        /// <summary>
        /// Executes an update check against the remote server.
        /// If force is false, respects the 48-hour throttle interval.
        /// </summary>
        public async Task CheckForUpdatesAsync(bool force = false)
        {
            if (Interlocked.CompareExchange(ref _isChecking, 1, 0) != 0)
            {
                return; // Already checking in another thread
            }

            try
            {
                var lastCheck = _settingsManager.GetLastUpdateCheckTime();
                if (!force && lastCheck.HasValue)
                {
                    var elapsedHours = (DateTime.Now - lastCheck.Value).TotalHours;
                    if (elapsedHours < CheckIntervalHours)
                    {
                        // 48 hours have not yet passed since last check
                        return;
                    }
                }

                _settingsManager.SetLastUpdateCheckTime(DateTime.Now);

                var currentVersion = System.Windows.Forms.Application.ProductVersion;
                var updateInfo = await _updateService.CheckForUpdatesAsync(currentVersion);

                if (updateInfo != null && updateInfo.IsNewerThan(currentVersion))
                {
                    NotifyUpdateAvailable(updateInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateSchedulerService] Update check failed: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isChecking, 0);
            }
        }

        private void NotifyUpdateAvailable(UpdateInfo updateInfo)
        {
            if (_parentForm == null || _parentForm.IsDisposed)
                return;

            // Marshall to UI thread safely
            if (_parentForm.InvokeRequired)
            {
                try
                {
                    _parentForm.BeginInvoke(() => NotifyUpdateAvailable(updateInfo));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateSchedulerService] BeginInvoke NotifyUpdateAvailable failed: {ex.Message}");
                }
                return;
            }

            // 1. Turn on visual update badge in navigation bar
            _setUpdateBadgeAction?.Invoke(true);

            // 2. Trigger desktop notification
            _showNotificationAction?.Invoke(updateInfo);
        }

        /// <summary>
        /// Prompts the user with the update dialog.
        /// </summary>
        public void PromptUserForUpdate(UpdateInfo updateInfo)
        {
            if (_parentForm == null || _parentForm.IsDisposed)
                return;

            if (_parentForm.InvokeRequired)
            {
                _parentForm.Invoke(() => PromptUserForUpdate(updateInfo));
                return;
            }

            bool isDark = _isDarkModeProvider?.Invoke() ?? false;
            _settingsManager.SetLastUpdatePromptTime(DateTime.Now);

            UpdateUIHandler.PromptAndDownloadUpdate(_parentForm, updateInfo, isDark, _downloadService, _packageVerifier);
        }

        public async Task CheckForNewContentAsync()
        {
            if (_rssFeedService == null) return;

            try
            {
                // Every post published after the last announced one, newest first.
                var newPosts = await _rssFeedService.GetUnseenNewPostsAsync();
                if (newPosts.Count > 0)
                {
                    NotifyNewContentAvailable(newPosts);

                    // Only the newest post is persisted; older ones sit after it in the feed and are
                    // therefore never reported again.
                    _rssFeedService.MarkPostAsNotified(newPosts[0]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateSchedulerService] New content check error: {ex.Message}");
            }
        }

        private void NotifyNewContentAvailable(IReadOnlyList<RssFeedItem> posts)
        {
            if (_parentForm == null || _parentForm.IsDisposed)
                return;

            if (_parentForm.InvokeRequired)
            {
                try
                {
                    _parentForm.BeginInvoke(() => NotifyNewContentAvailable(posts));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateSchedulerService] BeginInvoke NotifyNewContentAvailable failed: {ex.Message}");
                }
                return;
            }

            _showNewContentNotificationAction?.Invoke(posts);
        }

        public void Stop()
        {
            if (_periodicTimer != null)
            {
                _periodicTimer.Stop();
                _periodicTimer.Tick -= PeriodicTimer_Tick;
                _periodicTimer.Dispose();
                _periodicTimer = null;
            }
        }

        public void Dispose()
        {
            Stop();
            _parentForm = null;
            _setUpdateBadgeAction = null;
            _showNotificationAction = null;
            _showNewContentNotificationAction = null;
            _isDarkModeProvider = null;
        }
    }
}
