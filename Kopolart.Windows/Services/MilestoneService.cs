using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.UI.Forms;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Kopolart.Windows.Services
{
    public class MilestoneService : IDisposable
    {
        private readonly ISettingsManager _settingsManager;
        private readonly DateTime _sessionStartTime;
        private readonly Kopolart.Windows.Application.Services.UsageTrackerService? _usageTracker;
        private Timer? _milestoneTimer;
        private Form? _parentForm;
        private Func<bool>? _isDarkModeProvider;

        public MilestoneService(ISettingsManager settingsManager, DateTime sessionStartTime, Kopolart.Windows.Application.Services.UsageTrackerService? usageTracker = null)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _sessionStartTime = sessionStartTime;
            _usageTracker = usageTracker;
        }

        public void Start(Form parentForm, Func<bool> isDarkModeProvider)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));

            if (_milestoneTimer != null)
            {
                Stop();
            }

            _milestoneTimer = new Timer();
            _milestoneTimer.Interval = 10000; // Check every 10 seconds
            _milestoneTimer.Tick += MilestoneTimer_Tick;
            _milestoneTimer.Start();
        }

        public void Stop()
        {
            if (_milestoneTimer != null)
            {
                _milestoneTimer.Stop();
                _milestoneTimer.Tick -= MilestoneTimer_Tick;
                _milestoneTimer.Dispose();
                _milestoneTimer = null;
            }
        }

        private static readonly (TimeSpan Threshold, string Id, string MessageKey)[] MilestoneDefinitions =
        [
            (TimeSpan.FromMinutes(10), "10m", "Milestone10m"),
            (TimeSpan.FromHours(1),  "1h",  "Milestone1h"),
            (TimeSpan.FromHours(2),  "2h",  "Milestone2h"),
            (TimeSpan.FromHours(5),  "5h",  "Milestone5h"),
            (TimeSpan.FromHours(10), "10h", "Milestone10h"),
            (TimeSpan.FromHours(24), "24h", "Milestone24h"),
            (TimeSpan.FromHours(48), "48h", "Milestone48h"),
        ];

        private static readonly string[] AllMilestones = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(MilestoneDefinitions, m => m.Id));

        private void MilestoneTimer_Tick(object? sender, EventArgs e)
        {
            if (_parentForm == null || _parentForm.IsDisposed || _isDarkModeProvider == null)
            {
                Stop();
                return;
            }

            try
            {
                long totalTimeMs = _usageTracker != null
                    ? _usageTracker.GetTotalLifetimeUsageMilliseconds()
                    : (_settingsManager.GetTotalPastUsageMilliseconds() + (long)(DateTime.Now - _sessionStartTime).TotalMilliseconds);
                var totalElapsedSeconds = totalTimeMs / 1000.0;
                var totalElapsed = TimeSpan.FromSeconds(totalElapsedSeconds);

                var achieved = _settingsManager.GetAchievedMilestones();

                // Once every milestone is achieved there is nothing left to poll for.
                if (Array.TrueForAll(AllMilestones, achieved.Contains))
                {
                    Stop();
                    return;
                }

                string? milestoneToTrigger = null;
                string? messageKey = null;

                foreach (var (threshold, id, key) in MilestoneDefinitions)
                {
                    if (totalElapsed >= threshold && !achieved.Contains(id))
                    {
                        milestoneToTrigger = id;
                        messageKey = key;
                        break;
                    }
                }

                if (milestoneToTrigger != null && messageKey != null)
                {
                    _settingsManager.MarkMilestoneAchieved(milestoneToTrigger);
                    _settingsManager.Flush();

                    string msgText = TextManager.GetString(messageKey);
                    bool isDarkMode = _isDarkModeProvider();

                    // Pause the timer while the notification is sent
                    _milestoneTimer?.Stop();
                    try
                    {
                        var titleText = TextManager.GetString("MilestoneTitle");

                        new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                            .AddText(titleText)
                            .AddText(msgText)
                            .Show();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to show toast notification for milestone: {ex}");
                        // Fallback to MilestoneAlert if toast fails
                        using (var alert = new MilestoneAlert(isDarkMode, msgText))
                        {
                            alert.ShowDialog(_parentForm);
                        }
                    }
                    finally
                    {
                        if (_milestoneTimer != null && _parentForm != null && !_parentForm.IsDisposed)
                        {
                            _milestoneTimer.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking usage milestones: {ex}");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

