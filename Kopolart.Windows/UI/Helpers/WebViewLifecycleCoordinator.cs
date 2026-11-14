using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Controls;
using Kopolart.Windows.UI.Forms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Coordinates WebView2 lifecycle including runtime detection, installation,
    /// initialization with timeout, failure recovery, event subscriptions,
    /// suspend/resume on minimize, and loader view animation states.
    /// </summary>
    public class WebViewLifecycleCoordinator
    {
        private readonly Form _ownerForm;
        private readonly Panel _contentPanel;
        private readonly LoaderView _loaderView;
        private readonly Func<NavigationLoadingOverlay?> _navOverlayProvider;
        private readonly WebViewController _webViewController;
        private readonly IWebViewService _webViewService;
        private readonly ISettingsManager _settingsManager;
        private readonly ConnectionRetryHandler _retryHandler;
        private readonly LoadingStateManager _loadingState;
        private readonly UserSessionManager _sessionManager;
        private readonly UsageTrackerService _usageTracker;
        private readonly Func<bool> _isDarkModeProvider;

        private readonly Action<CoreWebView2DownloadStartingEventArgs> _onDownloadStarting;
        private readonly Action<bool> _onThemeChanged;
        private readonly Func<Task> _onCheckAndApplyDarkMode;
        private readonly Action<string> _onTitleChanged;
        private readonly Action _onForceExit;
        private readonly Action _onAfterStopLoading;
        private readonly Func<bool>? _isDashboardVisibleProvider;

        public WebViewLifecycleCoordinator(
            Form ownerForm,
            Panel contentPanel,
            LoaderView loaderView,
            Func<NavigationLoadingOverlay?> navOverlayProvider,
            WebViewController webViewController,
            IWebViewService webViewService,
            ISettingsManager settingsManager,
            ConnectionRetryHandler retryHandler,
            LoadingStateManager loadingState,
            UserSessionManager sessionManager,
            UsageTrackerService usageTracker,
            Func<bool> isDarkModeProvider,
            Action<CoreWebView2DownloadStartingEventArgs> onDownloadStarting,
            Action<bool> onThemeChanged,
            Func<Task> onCheckAndApplyDarkMode,
            Action<string> onTitleChanged,
            Action onForceExit,
            Action onAfterStopLoading,
            Func<bool>? isDashboardVisibleProvider = null)
        {
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
            _contentPanel = contentPanel ?? throw new ArgumentNullException(nameof(contentPanel));
            _loaderView = loaderView ?? throw new ArgumentNullException(nameof(loaderView));
            _navOverlayProvider = navOverlayProvider ?? throw new ArgumentNullException(nameof(navOverlayProvider));
            _webViewController = webViewController ?? throw new ArgumentNullException(nameof(webViewController));
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _retryHandler = retryHandler ?? throw new ArgumentNullException(nameof(retryHandler));
            _loadingState = loadingState ?? throw new ArgumentNullException(nameof(loadingState));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
            _onDownloadStarting = onDownloadStarting ?? throw new ArgumentNullException(nameof(onDownloadStarting));
            _onThemeChanged = onThemeChanged ?? throw new ArgumentNullException(nameof(onThemeChanged));
            _onCheckAndApplyDarkMode = onCheckAndApplyDarkMode ?? throw new ArgumentNullException(nameof(onCheckAndApplyDarkMode));
            _onTitleChanged = onTitleChanged ?? throw new ArgumentNullException(nameof(onTitleChanged));
            _onForceExit = onForceExit ?? throw new ArgumentNullException(nameof(onForceExit));
            _onAfterStopLoading = onAfterStopLoading ?? throw new ArgumentNullException(nameof(onAfterStopLoading));
            _isDashboardVisibleProvider = isDashboardVisibleProvider;

            WireRetryHandlerCallbacks();
            InitializeWebViewEvents();
        }

        private void WireRetryHandlerCallbacks()
        {
            _retryHandler.OnSetLoading = msg => _loaderView.SetLoading(msg);
            _retryHandler.OnShowError = (msg, btn, action) =>
            {
                _onAfterStopLoading();
                _loaderView.ShowError(msg, btn, action);
            };
            _retryHandler.OnStopLoading = () => StopLoadingAnimationAsync(true);
            _retryHandler.OnStartLoading = StartLoadingAnimation;
        }

        private void InitializeWebViewEvents()
        {
            _webViewController.DownloadStarting += (s, e) => _onDownloadStarting(e);
            _webViewController.BytesReceived += (s, bytes) => _usageTracker.TrackInternetBytes(bytes);

            _webViewController.ProcessFailed += (s, e) =>
            {
                Debug.WriteLine($"WebView2 process failed: {e.ProcessFailedKind}, Reason: {e.Reason}");
                if (_ownerForm.InvokeRequired)
                {
                    _ownerForm.Invoke(new Action(() => RecoverFromProcessFailure(e)));
                }
                else
                {
                    RecoverFromProcessFailure(e);
                }
            };

            _webViewController.ThemeChanged += (s, isDark) =>
            {
                if (_ownerForm.InvokeRequired)
                {
                    _ownerForm.BeginInvoke(new Action(() => _onThemeChanged(isDark)));
                }
                else
                {
                    _onThemeChanged(isDark);
                }
            };

            _webViewController.NavigationStarting += (s, e) =>
            {
                if (_retryHandler.IsInitialLoad)
                {
                    if (_isDashboardVisibleProvider?.Invoke() != true)
                    {
                        StartLoadingAnimation();
                    }
                }
                else if (_settingsManager.GetShowNavigationLoading() && e.NavigationKind != CoreWebView2NavigationKind.BackOrForward)
                {
                    _loadingState.StartNavigationLoading();
                    _navOverlayProvider()?.ShowOverlay();
                }
            };

            _webViewController.NavigationCompleted += async (s, e) =>
            {
                await _loadingState.EnforceMinimumDelayAsync();
                _navOverlayProvider()?.HideOverlay();
                try
                {
                    await _retryHandler.HandleNavigationCompletedAsync(_webViewController, e);

                    if (e.IsSuccess && !_retryHandler.IsInitialLoad)
                    {
                        await _onCheckAndApplyDarkMode();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in NavigationCompleted handler: {ex.Message}");
                }
            };

            _webViewController.RetryConnection += (s, e) =>
            {
                _retryHandler.HandleRetryConnection(_webViewController);
            };

            _webViewController.InitializationFailed += (s, ex) =>
            {
                if (_ownerForm.InvokeRequired)
                {
                    _ownerForm.BeginInvoke(new Action(() => HandleWebViewInitFailure(ex)));
                }
                else
                {
                    HandleWebViewInitFailure(ex);
                }
            };

            _webViewController.DocumentTitleChanged += (s, e) =>
            {
                if (_webViewController.WebView?.CoreWebView2 != null && _isDashboardVisibleProvider?.Invoke() != true)
                {
                    _onTitleChanged(_webViewController.DocumentTitle);
                }
            };

            _webViewController.InitializationCompleted += (s, e) =>
            {
                if (_isDashboardVisibleProvider?.Invoke() != true)
                {
                    _onTitleChanged(_webViewController.DocumentTitle);
                }
            };
        }

        public async Task StartInitialLoadAsync(bool showLoader = false)
        {
            if (showLoader)
            {
                StartLoadingAnimation();
            }
            _webViewController.EnableOfflinePage = false;

            try
            {
                if (_webViewService.IsRuntimeInstalled())
                {
                    await InitializeWebView2Async();
                }
                else
                {
                    await InstallWebView2Async();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during initialization: {ex}");
                await InstallWebView2Async();
            }
        }

        public void StartLoadingAnimation()
        {
            if (!_loadingState.IsUsageLogged)
            {
                _loadingState.StartInitialLoading();
                var elapsedSinceLastSession = _sessionManager.LogSessionStartAndGetElapsedSinceLastSession();
                if (elapsedSinceLastSession.HasValue)
                {
                    string durationText = DurationFormatter.FormatDuration(elapsedSinceLastSession.Value);
                    _loadingState.WelcomeMessage = string.Format(TextManager.GetString("LoaderWelcomeBack"), durationText);
                }
            }
            else
            {
                _loadingState.StartNavigationLoading();
            }

            _loaderView.StartLoadingAnimation(_loadingState.WelcomeMessage);
        }

        public async Task StopLoadingAnimationAsync(bool instant = false)
        {
            await _loadingState.EnforceMinimumDelayAsync();

            if (!_ownerForm.IsDisposed && !_loaderView.IsDisposed)
            {
                await _loaderView.StopLoadingAnimationAsync(instant);
            }

            _onAfterStopLoading();
        }

        private async Task InitializeWebView2Async()
        {
            try
            {
                var initTask = _webViewController.InitializeAsync(_contentPanel);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

                if (await Task.WhenAny(initTask, timeoutTask) == timeoutTask)
                {
                    ObserveAbandonedTask(initTask);

                    _loaderView.ShowError(
                        TextManager.GetString("LoaderInitTimeout"),
                        TextManager.GetString("LoaderRetry"),
                        () => _ = RetryWebViewInitializationAsync());
                }
                else
                {
                    await initTask;
                    if (_webViewController.WebView != null)
                    {
                        _webViewController.WebView.DefaultBackgroundColor = _isDarkModeProvider()
                            ? UIConstants.DarkMode.MenuStripBackColor
                            : Color.White;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during WebView2 initialization: {ex}");

                _loaderView.ShowError(
                    string.Format(TextManager.GetString("LoaderInitFailed"), ex.Message),
                    TextManager.GetString("LoaderRetry"),
                    () => _ = RetryWebViewInitializationAsync());
            }
        }

        private async Task InstallWebView2Async()
        {
            try
            {
                var progress = new Progress<int>(percent =>
                {
                    _loaderView.SetManualProgress(percent, TextManager.GetString("LoaderDownloading"));
                });

                Action<string> statusCallback = status =>
                {
                    _loaderView.ShowWarning(status);
                };

                _loaderView.ShowWarning(TextManager.GetString("LoaderInstallingWarning"));

                await _webViewController.InstallRuntimeAsync(progress, statusCallback);

                _webViewController.DisposeWebView();

                _loaderView.ShowSuccess(async () =>
                {
                    await InitializeWebView2Async();
                });
            }
            catch (Exception ex)
            {
                await StopLoadingAnimationAsync();
                Debug.WriteLine($"Failed to install WebView2: {ex.Message}");
                CustomMessageBox.Show(
                    _ownerForm,
                    TextManager.GetString("LoaderInstallFailed"),
                    TextManager.GetString("LoaderInstallFailedTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    _isDarkModeProvider());
                _onForceExit();
            }
        }

        private void HandleWebViewInitFailure(Exception? ex)
        {
            Debug.WriteLine($"WebView2 initialization failed: {ex}");
            _loaderView.ShowError(
                string.Format(TextManager.GetString("LoaderInitFailed"), ex?.Message ?? "Unknown"),
                TextManager.GetString("LoaderRetry"),
                () => _ = RetryWebViewInitializationAsync());
        }

        private async Task RetryWebViewInitializationAsync()
        {
            try
            {
                _webViewController.DisposeWebView();
                StartLoadingAnimation();
                await InitializeWebView2Async();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebView2 initialization retry failed: {ex}");
            }
        }

        private void RecoverFromProcessFailure(CoreWebView2ProcessFailedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"Attempting recovery after WebView2 process failure: {e.ProcessFailedKind}");
                _webViewController.Reload();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to recover from process failure: {ex.Message}");
            }
        }

        private static void ObserveAbandonedTask(Task task)
        {
            _ = task.ContinueWith(
                t => Debug.WriteLine($"Abandoned WebView2 init task completed. Faulted: {t.IsFaulted}, Error: {t.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public async Task HandleWindowStateChangeAsync(FormWindowState windowState)
        {
            if (windowState == FormWindowState.Minimized && _webViewController.WebView?.CoreWebView2 != null)
            {
                try { await _webViewController.WebView.CoreWebView2.TrySuspendAsync(); } catch { }
            }
            else if (windowState != FormWindowState.Minimized && _webViewController.WebView?.CoreWebView2 != null)
            {
                try { _webViewController.WebView.CoreWebView2.Resume(); } catch { }
            }
        }
    }
}

