﻿using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.Services;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Controls;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Kopolart.Windows.UI.Forms
{
    public partial class Form1 : Form, IMainFormActions
    {
        private readonly IWebViewService _webViewService;
        private readonly IUserService _userService;
        private readonly IAssetService _assetService;
        private readonly ISettingsManager _settingsManager;
        private readonly IUpdateService _updateService;
        private readonly IDownloadService _downloadService;
        private readonly IUpdatePackageVerifier _packageVerifier;
        private readonly IWallpaperService _wallpaperService;
        private readonly WebViewController _webViewController;
        private readonly ConnectionRetryHandler _retryHandler;
        private readonly NavigationLoadingOverlay? _navOverlay;
        private readonly LoadingStateManager _loadingState = new();

        private readonly Icon? appIcon;
        private bool systemMenuInitialized;
        private readonly DateTime _sessionStartTime = DateTime.Now;
        private bool _forceExit;

        private readonly MilestoneService _milestoneService;
        private readonly UserSessionManager _sessionManager;
        private readonly UsageTrackerService _usageTracker;
        private readonly UpdateSchedulerService _updateSchedulerService;
        private readonly PostNotificationService _postNotificationService = new();
        private readonly IBackgroundPostCheckScheduler _backgroundPostCheckScheduler;
        private Timer? _usageCommitTimer;

        private TrayIconManager? _trayIconManager;
        private AppNavigationBar _navBar = null!;
        private DashboardView _dashboardView = null!;
        private bool _isDashboardVisible = true;
        private string? _pendingNavigationUrl;

        // Single-responsibility coordinators
        private readonly KeyDeduplicator _keyDeduplicator = new();
        private readonly NetworkAvailabilityWatcher _networkWatcher;
        private readonly FormThemeCoordinator _themeCoordinator;
        private readonly WebViewDownloadCoordinator _downloadCoordinator;
        private readonly StartupUpdateCoordinator _updateCoordinator;
        private readonly WebViewLifecycleCoordinator _webViewLifecycle;
        private readonly FormClosingCoordinator _closingCoordinator;
        private readonly IUserIpLogger _userIpLogger;
        private readonly IAppVisitTrackerService _appVisitTrackerService;
        private readonly IDataBackupService _backupService;
        private readonly DataBackupCoordinator _backupCoordinator;

        public DateTime SessionStartTime => _sessionStartTime;
        public UsageTrackerService UsageTracker => _usageTracker;
        public IUserService UserService => _userService;
        public Form AsForm => this;
        public bool IsDarkMode => _themeCoordinator.IsDarkMode;
        public bool IsDashboardVisible => _isDashboardVisible;

        public Form1(
            IWebViewService webViewService,
            IAssetService assetService,
            ISettingsManager settingsManager,
            IUpdateService updateService,
            IDownloadService downloadService,
            IUpdatePackageVerifier packageVerifier,
            IDataBackupService backupService,
            IOnlineUsersService onlineUsersService,
            IAppVisitTrackerService appVisitTrackerService,
            IUserIpLogger userIpLogger,
            IRssFeedService rssFeedService,
            ISearchSuggestionService searchSuggestionService,
            IBackgroundPostCheckScheduler backgroundPostCheckScheduler,
            IWallpaperService wallpaperService)
        {
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
            _packageVerifier = packageVerifier ?? throw new ArgumentNullException(nameof(packageVerifier));
            _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _userIpLogger = userIpLogger ?? throw new ArgumentNullException(nameof(userIpLogger));
            _appVisitTrackerService = appVisitTrackerService ?? throw new ArgumentNullException(nameof(appVisitTrackerService));
            _backgroundPostCheckScheduler = backgroundPostCheckScheduler ?? throw new ArgumentNullException(nameof(backgroundPostCheckScheduler));

            _userService = new UserService(_webViewService);
            _userService.UserInfoChanged += OnUserInfoChanged;

            _webViewController = new WebViewController(_webViewService, _settingsManager, appVisitTrackerService);
            _usageTracker = new UsageTrackerService(_settingsManager, _sessionStartTime);
            _milestoneService = new MilestoneService(_settingsManager, _sessionStartTime, _usageTracker);
            _sessionManager = new UserSessionManager(_settingsManager, _sessionStartTime);
            _updateSchedulerService = new UpdateSchedulerService(_updateService, _settingsManager, _downloadService, _packageVerifier, rssFeedService);

            try
            {
                ToastNotificationManagerCompat.OnActivated += toastArgs =>
                {
                    var args = ToastArguments.Parse(toastArgs.Argument);
                    if (args.TryGetValue("action", out var action) && action == "open_post" && args.TryGetValue("url", out var url))
                    {
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            BeginInvoke(new Action(() =>
                            {
                                _trayIconManager?.RestoreFromTray();
                                WindowStateManager.RestoreAndFocus(this, _settingsManager);
                                ShowWebView(url);
                            }));
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Form1] Failed to register ToastNotificationManagerCompat.OnActivated: {ex.Message}");
            }

            _retryHandler = new ConnectionRetryHandler();

            InitializeComponent();
            FontManager.ApplyCustomFont(this);

            // Initialize Dashboard View
            _dashboardView = new DashboardView(
                _settingsManager,
                _usageTracker,
                _assetService,
                _updateService,
                _settingsManager.GetIsDarkMode(),
                onlineUsersService,
                rssFeedService,
                searchSuggestionService)
            {
                Dock = DockStyle.Fill
            };
            WireDashboardEvents();
            contentPanel.Controls.Add(_dashboardView);

            // Initialize coordinators
            loaderView.SetAssetService(_assetService);
            _themeCoordinator = new FormThemeCoordinator(
                this,
                contentPanel,
                loaderView,
                _webViewController,
                _settingsManager,
                () => _navOverlay,
                () => _navBar,
                () => _dashboardView);

            SystemAccessibility.Changed += OnSystemAccessibilityChanged;

            _backupCoordinator = new DataBackupCoordinator(
                this,
                _backupService,
                _settingsManager,
                _usageTracker,
                () => _themeCoordinator.IsDarkMode,
                isDark => UpdateThemeState(isDark),
                () =>
                {
                    _navBar?.RebuildBookmarksMenu();
                    _navBar?.UpdateBookmarkButtonState();
                    _dashboardView?.RefreshMetrics();
                    _dashboardView?.UpdateUserInfo(_userService.CurrentUser);
                });

            _downloadCoordinator = new WebViewDownloadCoordinator(
                this,
                _settingsManager,
                _usageTracker,
                () => _trayIconManager,
                () => _themeCoordinator.IsDarkMode);

            _updateCoordinator = new StartupUpdateCoordinator(
                _settingsManager,
                _updateSchedulerService,
                () => _themeCoordinator.IsDarkMode);

            _webViewLifecycle = new WebViewLifecycleCoordinator(
                this,
                contentPanel,
                loaderView,
                () => _navOverlay,
                _webViewController,
                _webViewService,
                _settingsManager,
                _retryHandler,
                _loadingState,
                _sessionManager,
                _usageTracker,
                () => _themeCoordinator.IsDarkMode,
                e => _downloadCoordinator.HandleDownloadStarting(e),
                isDark => _themeCoordinator.HandleThemeChanged(isDark),
                () => _themeCoordinator.ApplyAppThemeToWebContentAsync(),
                title =>
                {
                    if (!_isDashboardVisible)
                    {
                        Text = title;
                    }
                },
                ForceExit,
                () => _updateCoordinator.TryShowPendingUpdateNotification(this, () =>
                {
                    if (!_isDashboardVisible)
                    {
                        _webViewController.WebView?.Focus();
                    }
                }),
                () => _isDashboardVisible);

            _webViewController.InitializationCompleted += async (s, e) =>
            {
                await _userService.RefreshUserInfoAsync(_webViewController.WebView);
                if (!string.IsNullOrWhiteSpace(_pendingNavigationUrl))
                {
                    var url = _pendingNavigationUrl;
                    _pendingNavigationUrl = null;
                    _webViewController.WebView?.CoreWebView2?.Navigate(url);
                }
            };

            _webViewController.NavigationCompleted += async (s, e) =>
            {
                if (e.IsSuccess)
                {
                    await _userService.RefreshUserInfoAsync(_webViewController.WebView);
                }
            };

            _closingCoordinator = new FormClosingCoordinator(
                this,
                _settingsManager,
                _usageTracker,
                _webViewController,
                _webViewService,
                () => _trayIconManager,
                () => _themeCoordinator.IsDarkMode,
                StopUsageCommitTimer);

            _networkWatcher = new NetworkAvailabilityWatcher(this, () =>
            {
                if (_isDashboardVisible)
                {
                    _dashboardView?.RefreshMetrics();
                }

                if (_webViewController.IsShowingOfflinePage || _retryHandler.IsInitialLoad)
                {
                    Debug.WriteLine("Network connection restored, auto-reconnecting...");
                    _retryHandler.HandleRetryConnection(_webViewController);
                }
            });

            // Usage commit timer
            _usageCommitTimer = new Timer { Interval = 60000 };
            _usageCommitTimer.Tick += (s, e) =>
            {
                _usageTracker.CommitElapsed(DateTime.Now);
                if (_isDashboardVisible)
                {
                    _dashboardView.RefreshMetrics();
                }
            };
            _usageCommitTimer.Start();

            // Navigation bar
            _navBar = new AppNavigationBar(
                this,
                _webViewController,
                _settingsManager,
                _webViewService,
                _updateService,
                () => _themeCoordinator.IsDarkMode,
                _downloadService,
                _packageVerifier,
                _wallpaperService,
                _backupCoordinator);
            Controls.Add(_navBar);

            appIcon = _assetService.GetAppIcon();
            if (appIcon != null)
            {
                Icon = appIcon;
            }

            _trayIconManager = new TrayIconManager(this, _settingsManager, appIcon);

            // Window state
            WindowStateManager.LoadWindowState(this, _settingsManager);
            if (_settingsManager.GetStartMaximized() ||
                (WindowState == FormWindowState.Normal && Size is { Width: UIConstants.DefaultWindowWidth, Height: UIConstants.DefaultWindowHeight }))
            {
                WindowState = FormWindowState.Maximized;
            }

            KeyPreview = true;

            _navOverlay = new NavigationLoadingOverlay();
            _navOverlay.SetAssetService(_assetService);
            contentPanel.Controls.Add(_navOverlay);

            _themeCoordinator.ApplyDarkModeToUI(_themeCoordinator.IsDarkMode);

            _webViewController.AcceleratorKeyHandler = keyData =>
                _keyDeduplicator.DeduplicateKey(keyData, () =>
                    KeyboardShortcutHandler.ProcessKey(
                        keyData,
                        _webViewController,
                        () =>
                        {
                            if (_isDashboardVisible)
                            {
                                _closingCoordinator.HandleBackOrExit();
                            }
                            else if (_webViewController.CanGoBack)
                            {
                                _webViewController.GoBack();
                            }
                            else
                            {
                                ShowDashboard();
                            }
                        },
                        _navBar.ToggleCurrentPageBookmark,
                        _navBar.PerformHardRefresh,
                        _navBar.OpenHistoryDialog,
                        _navBar.OpenDownloadsDialog,
                        ToggleDashboard));

            InitializeTexts();
            _milestoneService.Start(this, () => _themeCoordinator.IsDarkMode);
        }

        private void InitializeTexts()
        {
            ApplyTexts();
        }

        private void StopUsageCommitTimer()
        {
            if (_usageCommitTimer != null)
            {
                _usageCommitTimer.Stop();
                _usageCommitTimer.Dispose();
                _usageCommitTimer = null;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.SetImmersiveDarkMode(Handle, _themeCoordinator.IsDarkMode);
            if (!systemMenuInitialized)
            {
                _navBar.ApplySystemMenuTexts();
                systemMenuInitialized = true;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            var args = Environment.GetCommandLineArgs();
            bool isAutoStart = args is { Length: > 1 } && Array.Exists(args, arg => arg.Equals("--autostart", StringComparison.OrdinalIgnoreCase));
            if (isAutoStart && _settingsManager.GetMinimizeToTrayEnabled())
            {
                WindowState = FormWindowState.Minimized;
                _trayIconManager?.HideToTray();
                return;
            }

            bool isUpdated = args is { Length: > 1 } && Array.Exists(args, arg => arg.Equals("--updated", StringComparison.OrdinalIgnoreCase));
            if (WindowState == FormWindowState.Minimized)
            {
                WindowStateManager.RestoreAndFocus(this, _settingsManager);
            }
            else if (isUpdated)
            {
                BringToFront();
                Activate();
                NativeMethods.SetForegroundWindow(Handle);
            }

            if (_userIpLogger != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _userIpLogger.LogUserIpAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to log user IP on startup: {ex.Message}");
                    }
                });
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)NativeMethods.WM_SHOW_KOPOLART)
            {
                _trayIconManager?.RestoreFromTray();
                WindowStateManager.RestoreAndFocus(this, _settingsManager);

                if (m.WParam == (IntPtr)1)
                {
                    ShowDashboard();
                }
                else if (m.WParam == (IntPtr)2)
                {
                    _navBar.OpenDownloadsDialog();
                }
                else if (m.WParam == (IntPtr)3)
                {
                    ShowWebView();
                }
                return;
            }

            if (m.Msg == SystemMenuHelper.WM_SYSCOMMAND)
            {
                var commandId = (int)m.WParam & 0xFFF0;
                if (commandId == SystemMenuHelper.StartMaximizedSysMenuId)
                {
                    var newValue = SystemMenuHelper.ToggleStartMaximized(this, _settingsManager.GetStartMaximized());
                    _settingsManager.SetStartMaximized(newValue);
                    return;
                }
                if (commandId == SystemMenuHelper.ShowNavLoadingSysMenuId)
                {
                    var newValue = SystemMenuHelper.ToggleShowNavLoading(this, _settingsManager.GetShowNavigationLoading());
                    _settingsManager.SetShowNavigationLoading(newValue);
                    return;
                }
            }

            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _closingCoordinator.HandleFormClosing(e, _forceExit);
            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_keyDeduplicator.DeduplicateKey(keyData, () =>
                KeyboardShortcutHandler.ProcessKey(
                    keyData,
                    _webViewController,
                    () =>
                    {
                        if (_isDashboardVisible)
                        {
                            _closingCoordinator.HandleBackOrExit();
                        }
                        else if (_webViewController.CanGoBack)
                        {
                            _webViewController.GoBack();
                        }
                        else
                        {
                            ShowDashboard();
                        }
                    },
                    _navBar.ToggleCurrentPageBookmark,
                    _navBar.PerformHardRefresh,
                    _navBar.OpenHistoryDialog,
                    _navBar.OpenDownloadsDialog,
                    ToggleDashboard)))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            Resize += async (s, args) =>
            {
                try
                {
                    WindowStateManager.SaveWindowState(this, _settingsManager);
                    await _webViewLifecycle.HandleWindowStateChangeAsync(WindowState);
                    if (WindowState == FormWindowState.Minimized)
                    {
                        _usageTracker.PauseTracking();
                    }
                    else if (!_isDashboardVisible && Visible)
                    {
                        _usageTracker.ResumeTracking();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Form1] Window state change error: {ex}");
                }
            };
            Move += (s, args) => WindowStateManager.SaveWindowState(this, _settingsManager);

            try
            {
                var logo = _assetService.LoadLogo(_themeCoordinator.IsDarkMode);
                if (logo != null)
                {
                    loaderView.SetLogo(logo);
                    _navOverlay?.SetLogo((Image)logo.Clone());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load logo image: {ex}");
            }

            var args = Environment.GetCommandLineArgs();
            bool hasCustomJumpArg = false;
            if (args is { Length: > 1 })
            {
                if (Array.Exists(args, a => a.Equals("--downloads", StringComparison.OrdinalIgnoreCase)))
                {
                    hasCustomJumpArg = true;
                    loaderView.Visible = false;
                    loaderView.SendToBack();
                    ShowDashboard();
                    _ = _webViewLifecycle.StartInitialLoadAsync(showLoader: false);
                    BeginInvoke(new Action(() => _navBar.OpenDownloadsDialog()));
                }
                else if (Array.Exists(args, a => a.Equals("--last-page", StringComparison.OrdinalIgnoreCase)))
                {
                    hasCustomJumpArg = true;
                    ShowWebView();
                    await _webViewLifecycle.StartInitialLoadAsync(showLoader: true);
                }
                else if (Array.Exists(args, a => a.Equals("--dashboard", StringComparison.OrdinalIgnoreCase)))
                {
                    hasCustomJumpArg = true;
                    loaderView.Visible = false;
                    loaderView.SendToBack();
                    ShowDashboard();
                    await _webViewLifecycle.StartInitialLoadAsync(showLoader: false);
                }
            }

            if (!hasCustomJumpArg)
            {
                if (_settingsManager.GetOpenDashboardAtStartup())
                {
                    loaderView.Visible = false;
                    loaderView.SendToBack();
                    ShowDashboard();
                    await _webViewLifecycle.StartInitialLoadAsync(showLoader: false);
                }
                else
                {
                    ShowWebView();
                    await _webViewLifecycle.StartInitialLoadAsync(showLoader: true);
                }
            }

            _updateCoordinator.ProcessStartupArguments(Environment.GetCommandLineArgs());
            _updateCoordinator.StartUpdateScheduler(
                this,
                badge =>
                {
                    _navBar.SetUpdateAvailable(badge);
                    _dashboardView.SetUpdateBadge(badge);
                },
                () => _trayIconManager,
                posts => OnNewFeedContentDispatched(posts));
        }

        public void UpdateThemeState(bool isDark) => _themeCoordinator.UpdateThemeState(isDark);
        public void ApplyDarkModeToUI(bool isDark) => _themeCoordinator.ApplyDarkModeToUI(isDark);

        private void OnSystemAccessibilityChanged(object? sender, SystemVisualSettingsChangedEventArgs e)
        {
            _themeCoordinator.ApplyDarkModeToUI(_themeCoordinator.IsDarkMode);
        }

        public void ApplyTexts()
        {
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            _navBar?.ApplyTexts();
            _dashboardView?.ApplyTexts();
            _trayIconManager?.ApplyTexts();

            FontManager.ApplyCustomFont(this);
            PerformLayout();

            if (_webViewController.IsShowingOfflinePage)
            {
                _webViewController.Reload();
            }

            UpdateWindowTitle();

            if (IsHandleCreated && Visible && !_isDashboardVisible)
            {
                Activate();
                Focus();
                BeginInvoke(new Action(() =>
                {
                    Activate();
                    Focus();
                    _webViewController.WebView?.Focus();
                }));
            }
        }

        private void OnUserInfoChanged(UserInfo? user)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnUserInfoChanged(user)));
                return;
            }
            _dashboardView?.UpdateUserInfo(user);
            loaderView.CustomUserName = _userService.GetDisplayName();
        }

        private void WireDashboardEvents()
        {
            _dashboardView.StartBrowsingRequested += (s, e) => ShowWebView();
            _dashboardView.SearchRequested += (s, url) => ShowWebView(url);
            _dashboardView.CategoryRequested += (s, url) => ShowWebView(url);
            _dashboardView.PersonalWebsiteRequested += (s, e) =>
            {
                _ = _appVisitTrackerService?.TrackVisitAsync(AppConstants.PartialVisitKeys.JosephyBrowse);
                WebView2SecurityManager.TryOpenExternalUri("https://josephy.ir");
            };
            _dashboardView.UserProfileRequested += (s, e) =>
            {
                if (_userService.CurrentUser != null && _userService.CurrentUser.UserId > 0)
                {
                    ShowWebView($"{AppConstants.Urls.UserProfileBase}{_userService.CurrentUser.UserId}");
                }
                else
                {
                    ShowWebView(AppConstants.Urls.UserLogin);
                }
            };
            _dashboardView.LoginRequested += (s, e) => ShowWebView(AppConstants.Urls.UserLogin);
            _dashboardView.RegisterRequested += (s, e) => ShowWebView(AppConstants.Urls.UserRegister);
            _dashboardView.SettingsRequested += (s, e) => OpenSettingsDialog();
            _dashboardView.UsageRequested += (s, e) => OpenDailyUsageDialog();
            _dashboardView.HistoryRequested += (s, e) => _navBar.OpenHistoryDialog();
            _dashboardView.DownloadsRequested += (s, e) => _navBar.OpenDownloadsDialog();
            _dashboardView.BookmarksRequested += (s, e) => _navBar.OpenManageBookmarksDialog();
            _dashboardView.ShortcutsRequested += (s, e) => OpenShortcutsDialog();
            _dashboardView.ContactRequested += (s, e) => ShowWebView($"https://{AppConstants.Urls.Domain}/contact-us");
            _dashboardView.AboutRequested += (s, e) => ShowAboutDialog();
        }

        private void OnNewFeedContentDispatched(IReadOnlyList<RssFeedItem> posts)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnNewFeedContentDispatched(posts)));
                return;
            }

            if (posts == null || posts.Count == 0)
                return;

            bool toastShown = _postNotificationService.ShowDigest(posts, appIsRunning: true);

            if (!toastShown)
            {
                // Toast API unavailable: fall back to a tray balloon with the same wording.
                var content = _postNotificationService.CreateContent(posts);
                _trayIconManager?.ShowNewContentNotification(content.Title, content.Body, () =>
                {
                    ShowWebView(content.PrimaryUrl);
                });
            }
        }

        public void ShowDashboard()
        {
            _usageTracker.PauseTracking();
            _isDashboardVisible = true;
            _dashboardView.Visible = true;
            _dashboardView.BringToFront();
            _dashboardView.RefreshMetrics();
            _dashboardView.RefreshRecentSearches();
            _ = _userService.RefreshUserInfoAsync(_webViewController.WebView);
            _dashboardView.UpdateUserInfo(_userService.CurrentUser);
            _navBar.UpdateNavButtons();
            UpdateWindowTitle();
        }

        public void ShowWebView(string? targetUrl = null)
        {
            _isDashboardVisible = false;
            _dashboardView.Visible = false;
            if (WindowState != FormWindowState.Minimized && Visible)
            {
                _usageTracker.ResumeTracking();
            }

            _sessionManager.LogSessionStartAndGetElapsedSinceLastSession();

            if (!string.IsNullOrWhiteSpace(targetUrl))
            {
                if (_webViewController.WebView?.CoreWebView2 != null)
                {
                    _webViewController.WebView.CoreWebView2.Navigate(targetUrl);
                }
                else
                {
                    _pendingNavigationUrl = targetUrl;
                }
            }

            if (_retryHandler.IsInitialLoad)
            {
                _webViewLifecycle.StartLoadingAnimation();
                loaderView.Visible = true;
                loaderView.BringToFront();
            }
            else
            {
                _webViewController.WebView?.BringToFront();
                _webViewController.WebView?.Focus();
            }

            _navBar.UpdateNavButtons();
            UpdateWindowTitle();
        }

        private void ToggleDashboard()
        {
            if (_isDashboardVisible)
            {
                ShowWebView();
            }
            else
            {
                ShowDashboard();
            }
        }

        private void UpdateWindowTitle()
        {
            if (_isDashboardVisible)
            {
                string dashboardTitle = TextManager.GetString("DashboardTitle");
                Text = $"{dashboardTitle} - {UIConstants.AppName}";
            }
            else
            {
                Text = !string.IsNullOrWhiteSpace(_webViewController.DocumentTitle)
                    ? _webViewController.DocumentTitle
                    : UIConstants.AppName;
            }
        }

        private void OpenDailyUsageDialog()
        {
            using var logsDialog = new AppLogsDialog(
                _settingsManager,
                _themeCoordinator.IsDarkMode,
                SessionStartTime,
                UsageTracker,
                initialTab: 0);
            logsDialog.ShowDialog(this);
        }

        private void OpenShortcutsDialog()
        {
            using var dialog = new ShortcutsDialog(_themeCoordinator.IsDarkMode);
            dialog.ShowDialog(this);
        }

        private void ShowAboutDialog()
        {
            var version = System.Windows.Forms.Application.ProductVersion;
            string format = TextManager.GetString("AboutText");
            string aboutText = string.Format(format, version);
            string aboutTitle = TextManager.GetString("MenuAbout");
            CustomMessageBox.Show(this, aboutText, aboutTitle, MessageBoxButtons.OK, MessageBoxIcon.Information, _themeCoordinator.IsDarkMode);
        }

        public void PromptExit() => Close();

        public void ForceExit()
        {
            _forceExit = true;
            Close();
        }

        public void OpenSettingsDialog()
        {
            using var settingsDialog = new SettingsDialog(
                _settingsManager,
                _webViewService,
                _webViewController,
                _themeCoordinator.IsDarkMode,
                _backupCoordinator,
                () => _postNotificationService.ShowTest());
            if (settingsDialog.ShowDialog(this) == DialogResult.OK)
            {
                _dashboardView?.RefreshMetrics();

                // Keep the background (app-closed) post check in sync with the notification switch.
                _backgroundPostCheckScheduler?.Sync(_settingsManager.GetNotifyNewContent());
            }
        }

        public void CheckForUpdatesManually()
        {
            _navBar?.CheckForUpdatesManualAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemAccessibility.Changed -= OnSystemAccessibilityChanged;
                _networkWatcher?.Dispose();

                if (components != null)
                {
                    components.Dispose();
                }

                _milestoneService?.Dispose();
                _updateSchedulerService?.Dispose();
                _webViewController?.Dispose();
                _dashboardView?.Dispose();
                appIcon?.Dispose();
                _trayIconManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

