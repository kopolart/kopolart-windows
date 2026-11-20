using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kopolart.Windows.Application.Services
{
    /// <summary>
    /// Tracks application usage duration in real-time, safely partitioning elapsed
    /// time across midnight boundaries into daily buckets and updating cumulative past usage.
    /// </summary>
    public class UsageTrackerService
    {
        private readonly ISettingsManager _settingsManager;
        private readonly object _syncLock = new();
        private readonly System.Diagnostics.Stopwatch _browsingStopwatch = new();
        private DateTime _lastTrackedTime;
        private bool _isTrackingActive;

        public DateTime LastTrackedTime
        {
            get { lock (_syncLock) return _lastTrackedTime; }
        }

        public bool IsTrackingActive
        {
            get { lock (_syncLock) return _isTrackingActive; }
        }

        public UsageTrackerService(ISettingsManager settingsManager, DateTime? startTime = null)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _lastTrackedTime = startTime ?? DateTime.Now;
            _isTrackingActive = false;
        }

        /// <summary>
        /// Resumes or starts tracking webview usage time.
        /// </summary>
        public void ResumeTracking(DateTime? currentTime = null)
        {
            var now = currentTime ?? DateTime.Now;
            lock (_syncLock)
            {
                if (_isTrackingActive) return;
                _isTrackingActive = true;
                _lastTrackedTime = now;
                if (!_browsingStopwatch.IsRunning)
                {
                    _browsingStopwatch.Start();
                }
            }
        }

        /// <summary>
        /// Pauses tracking webview usage time and commits any in-flight active time.
        /// </summary>
        public void PauseTracking(DateTime? currentTime = null)
        {
            var now = currentTime ?? DateTime.Now;
            lock (_syncLock)
            {
                if (!_isTrackingActive) return;
                CommitElapsedInternal(now);
                _isTrackingActive = false;
                if (_browsingStopwatch.IsRunning)
                {
                    _browsingStopwatch.Stop();
                }
                _settingsManager.UpdateLastUsageDuration((long)_browsingStopwatch.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Commits all elapsed time since the last tracking point into the daily usage store
        /// and cumulative total usage if tracking is currently active. Correctly splits any interval that crosses midnight.
        /// </summary>
        public void CommitElapsed(DateTime? currentTime = null)
        {
            var now = currentTime ?? DateTime.Now;

            lock (_syncLock)
            {
                if (!_isTrackingActive)
                {
                    return;
                }

                CommitElapsedInternal(now);
                _settingsManager.UpdateLastUsageDuration((long)_browsingStopwatch.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Gets the total active browsing duration in the current session.
        /// </summary>
        public TimeSpan GetSessionBrowsingDuration()
        {
            lock (_syncLock)
            {
                return _browsingStopwatch.Elapsed;
            }
        }

        private void CommitElapsedInternal(DateTime now)
        {
            if (now <= _lastTrackedTime)
            {
                return;
            }

            var current = _lastTrackedTime;

            while (current.Date < now.Date)
            {
                var nextMidnight = current.Date.AddDays(1);
                var chunkMs = (long)(nextMidnight - current).TotalMilliseconds;
                if (chunkMs > 0)
                {
                    var dateKey = current.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    _settingsManager.AddDailyUsage(dateKey, chunkMs);
                    _settingsManager.SetTotalPastUsageMilliseconds(_settingsManager.GetTotalPastUsageMilliseconds() + chunkMs);
                }
                current = nextMidnight;
            }

            if (now > current)
            {
                var remainderMs = (long)(now - current).TotalMilliseconds;
                if (remainderMs > 0)
                {
                    var dateKey = current.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    _settingsManager.AddDailyUsage(dateKey, remainderMs);
                    _settingsManager.SetTotalPastUsageMilliseconds(_settingsManager.GetTotalPastUsageMilliseconds() + remainderMs);
                }
            }

            _lastTrackedTime = now;
        }

        /// <summary>
        /// Gets an up-to-the-second snapshot of all daily usage records, including
        /// uncommitted in-flight elapsed time up to <paramref name="currentTime"/> if tracking is active.
        /// </summary>
        public Dictionary<string, long> GetLiveDailyUsage(DateTime? currentTime = null)
        {
            var now = currentTime ?? DateTime.Now;
            Dictionary<string, long> liveCopy;

            lock (_syncLock)
            {
                var stored = _settingsManager.GetDailyUsage();
                liveCopy = new Dictionary<string, long>(stored, StringComparer.OrdinalIgnoreCase);

                if (_isTrackingActive && now > _lastTrackedTime)
                {
                    var current = _lastTrackedTime;
                    while (current.Date < now.Date)
                    {
                        var nextMidnight = current.Date.AddDays(1);
                        var chunkMs = (long)(nextMidnight - current).TotalMilliseconds;
                        if (chunkMs > 0)
                        {
                            var dateKey = current.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                            liveCopy[dateKey] = liveCopy.GetValueOrDefault(dateKey, 0L) + chunkMs;
                        }
                        current = nextMidnight;
                    }

                    if (now > current)
                    {
                        var remainderMs = (long)(now - current).TotalMilliseconds;
                        if (remainderMs > 0)
                        {
                            var dateKey = current.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                            liveCopy[dateKey] = liveCopy.GetValueOrDefault(dateKey, 0L) + remainderMs;
                        }
                    }
                }
            }

            return liveCopy;
        }

        /// <summary>
        /// Gets the exact total lifetime usage in milliseconds, combining committed
        /// past usage and any uncommitted active in-flight slice if tracking is active.
        /// </summary>
        public long GetTotalLifetimeUsageMilliseconds(DateTime? currentTime = null)
        {
            var now = currentTime ?? DateTime.Now;
            lock (_syncLock)
            {
                var uncommitted = (_isTrackingActive && now > _lastTrackedTime) ? (long)(now - _lastTrackedTime).TotalMilliseconds : 0L;
                return _settingsManager.GetTotalPastUsageMilliseconds() + uncommitted;
            }
        }

        /// <summary>
        /// Records consumed internet data bytes.
        /// </summary>
        public void TrackInternetBytes(long bytes)
        {
            if (bytes <= 0) return;
            _settingsManager.AddInternetBytes(bytes);
        }

        /// <summary>
        /// Gets the total lifetime internet bytes recorded.
        /// </summary>
        public long GetTotalLifetimeInternetBytes()
        {
            return _settingsManager.GetTotalInternetBytes();
        }

        /// <summary>
        /// Gets the internet bytes recorded for today.
        /// </summary>
        public long GetTodayInternetBytes()
        {
            var dict = _settingsManager.GetDailyInternetBytes();
            var todayKey = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return dict != null && dict.TryGetValue(todayKey, out var b) ? b : 0L;
        }

        /// <summary>
        /// Gets daily breakdown of internet bytes.
        /// </summary>
        public Dictionary<string, long> GetDailyInternetBytes()
        {
            return _settingsManager.GetDailyInternetBytes();
        }

        /// <summary>
        /// Formats byte count into Persian human-readable data size string (e.g. "۲ مگابایت").
        /// </summary>
        public static string FormatDataSize(long bytes)
        {
            if (bytes <= 0) return "۰ بایت";

            const double kb = 1024.0;
            const double mb = 1024.0 * 1024.0;
            const double gb = 1024.0 * 1024.0 * 1024.0;

            if (bytes < kb)
            {
                return $"{PersianNumberHelper.ToPersianDigits(bytes.ToString())} بایت";
            }
            if (bytes < mb)
            {
                double val = bytes / kb;
                return $"{PersianNumberHelper.ToPersianDigits(val.ToString("0.#"))} کیلوبایت";
            }
            if (bytes < gb)
            {
                double val = bytes / mb;
                return $"{PersianNumberHelper.ToPersianDigits(val.ToString("0.#"))} مگابایت";
            }
            double valGb = bytes / gb;
            return $"{PersianNumberHelper.ToPersianDigits(valGb.ToString("0.##"))} گیگابایت";
        }
    }
}
