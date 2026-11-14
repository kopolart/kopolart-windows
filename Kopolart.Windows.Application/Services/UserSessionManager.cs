using Kopolart.Windows.Application.Interfaces;
using System;
using System.Diagnostics;

namespace Kopolart.Windows.Application.Services
{
    public class UserSessionManager
    {
        private readonly ISettingsManager _settingsManager;
        private readonly DateTime _sessionStartTime;
        private bool _isUsageLogged = false;

        public bool IsUsageLogged => _isUsageLogged;

        public UserSessionManager(ISettingsManager settingsManager, DateTime sessionStartTime)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _sessionStartTime = sessionStartTime;
        }

        /// <summary>
        /// Logs the start of the session and returns the time elapsed since the last
        /// session, or null on the first run (or if already logged). Formatting the
        /// "Welcome back" message from this value is the UI layer's responsibility —
        /// the Application layer no longer produces localized display strings.
        /// </summary>
        public TimeSpan? LogSessionStartAndGetElapsedSinceLastSession()
        {
            if (_isUsageLogged) return null;
            _isUsageLogged = true;

            TimeSpan? elapsed = null;
            try
            {
                var lastClosed = _settingsManager.GetLastClosedTime();

                if (lastClosed.HasValue)
                {
                    elapsed = DateTime.Now - lastClosed.Value;
                }
                else
                {
                    var usageLog = _settingsManager.GetUsageLog();
                    if (usageLog is { Count: > 0 })
                    {
                        var lastUsage = usageLog[^1];
                        elapsed = DateTime.Now - lastUsage.Date;
                    }
                }

                _settingsManager.LogUsage(DateTime.Now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log usage: {ex.Message}");
            }

            return elapsed;
        }
    }
}
