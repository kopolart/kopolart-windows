using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.Services;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Forms;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Coordinates the application closing and shutdown sequence, including user exit prompt dialog,
    /// minimize-to-tray handling, final window state cleanup, usage tracking commit,
    /// last visited URL persistence, cache clearing, and settings flush.
    /// </summary>
    public class FormClosingCoordinator
    {
        private readonly Form _ownerForm;
        private readonly ISettingsManager _settingsManager;
        private readonly UsageTrackerService _usageTracker;
        private readonly WebViewController _webViewController;
        private readonly IWebViewService _webViewService;
        private readonly Func<TrayIconManager?> _trayIconManagerProvider;
        private readonly Func<bool> _isDarkModeProvider;
        private readonly Action _onStopUsageCommitTimer;

        private bool _exitPromptOpen;

        public FormClosingCoordinator(
            Form ownerForm,
            ISettingsManager settingsManager,
            UsageTrackerService usageTracker,
            WebViewController webViewController,
            IWebViewService webViewService,
            Func<TrayIconManager?> trayIconManagerProvider,
            Func<bool> isDarkModeProvider,
            Action onStopUsageCommitTimer)
        {
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
            _webViewController = webViewController ?? throw new ArgumentNullException(nameof(webViewController));
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
            _trayIconManagerProvider = trayIconManagerProvider ?? throw new ArgumentNullException(nameof(trayIconManagerProvider));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
            _onStopUsageCommitTimer = onStopUsageCommitTimer ?? throw new ArgumentNullException(nameof(onStopUsageCommitTimer));
        }

        public void HandleFormClosing(FormClosingEventArgs e, bool forceExit)
        {
            var trayIconManager = _trayIconManagerProvider();

            if (!forceExit && e.CloseReason == CloseReason.UserClosing)
            {
                if (_settingsManager.GetShowExitPrompt())
                {
                    if (_exitPromptOpen)
                    {
                        e.Cancel = true;
                        return;
                    }

                    _exitPromptOpen = true;
                    try
                    {
                        bool defaultMinimize = _settingsManager.GetMinimizeToTrayEnabled();
                        using (var exitDialog = new ExitDialog(_isDarkModeProvider(), defaultMinimize))
                        {
                            var result = exitDialog.ShowDialog(_ownerForm);

                            if (result == DialogResult.Yes)
                            {
                                _settingsManager.SetMinimizeToTrayEnabled(exitDialog.MinimizeToTray);
                                _settingsManager.SetShowExitPrompt(!exitDialog.DontAskAgain);
                                Task.Run(() => _settingsManager.Flush());

                                if (exitDialog.MinimizeToTray)
                                {
                                    e.Cancel = true;
                                    trayIconManager?.HideToTray();
                                    return;
                                }
                            }
                            else
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                    }
                    finally
                    {
                        _exitPromptOpen = false;
                    }
                }
                else
                {
                    if (_settingsManager.GetMinimizeToTrayEnabled())
                    {
                        e.Cancel = true;
                        trayIconManager?.HideToTray();
                        return;
                    }
                }
            }

            WindowStateManager.Cleanup();

            _onStopUsageCommitTimer();

            try
            {
                _usageTracker.PauseTracking(DateTime.Now);
                _settingsManager.SetLastClosedTime(DateTime.Now);

                // Safely read URL from UI thread before shutting down
                var currentUrl = _webViewController.WebView?.Source?.ToString();
                if (!string.IsNullOrWhiteSpace(currentUrl) && currentUrl != "about:blank")
                {
                    _settingsManager.SetLastVisitedUrl(currentUrl);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save total usage time or last visited URL on closing: {ex}");
            }

            if (_settingsManager.GetClearCacheOnExit())
            {
                try
                {
                    if (_webViewController.WebView?.CoreWebView2 != null)
                    {
                        var clearTask = _webViewService.ClearCacheOnlyAsync(_webViewController.WebView);
                        var sw = Stopwatch.StartNew();
                        while (!clearTask.IsCompleted && sw.ElapsedMilliseconds < 3000)
                        {
                            System.Windows.Forms.Application.DoEvents();
                            Thread.Sleep(10);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to clear cache on exit: {ex}");
                }
            }

            // Flush synchronously on shutdown to ensure settings are persisted before process exits
            _settingsManager.Flush();

            trayIconManager?.Dispose();
        }

        public void HandleBackOrExit()
        {
            if (_webViewController.CanGoBack)
            {
                _webViewController.GoBack();
            }
            else
            {
                _ownerForm.Close();
            }
        }
    }
}
