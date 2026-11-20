using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Controls.NavBar;
using Kopolart.Windows.UI.Forms;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    /// <summary>
    /// Top-level application navigation bar providing browser controls, menu strip,
    /// bookmarks, and theme coordination.
    /// </summary>
    public class AppNavigationBar : UserControl
    {
        private readonly IMainFormActions _parentForm;
        private readonly WebViewController _webViewController;
        private readonly ISettingsManager _settingsManager;
        private readonly IWebViewService _webViewService;
        private readonly IUpdateService _updateService;
        private readonly Func<bool> _isDarkModeProvider;
        private readonly IDownloadService _downloadService;
        private readonly IUpdatePackageVerifier _packageVerifier;
        private readonly IWallpaperService _wallpaperService;
        private readonly DataBackupCoordinator? _backupCoordinator;

        private MenuStrip _menuStrip = null!;
        private NavBarMenuItems _menuItems = null!;
        private NavigationMenuDismissalHandler _dismissalHandler = null!;
        private NavBarBookmarksHandler _bookmarksHandler = null!;
        private NavBarUpdateHandler _updateHandler = null!;
        private NavBarNavigationButtonsHandler _navigationButtonsHandler = null!;
        private NavBarTextHandler _textHandler = null!;

        public AppNavigationBar(
            IMainFormActions parentForm,
            WebViewController webViewController,
            ISettingsManager settingsManager,
            IWebViewService webViewService,
            IUpdateService updateService,
            Func<bool> isDarkModeProvider,
            IDownloadService downloadService,
            IUpdatePackageVerifier packageVerifier,
            IWallpaperService wallpaperService,
            DataBackupCoordinator? backupCoordinator = null)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _webViewController = webViewController ?? throw new ArgumentNullException(nameof(webViewController));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
            _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
            _packageVerifier = packageVerifier ?? throw new ArgumentNullException(nameof(packageVerifier));
            _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            _backupCoordinator = backupCoordinator;

            InitializeBar();
        }

        private void InitializeBar()
        {
            _menuStrip = new MenuStrip
            {
                Name = "menuStrip1",
                Text = "menuStrip1",
                Dock = DockStyle.None,
                AutoSize = true,
                ShowItemToolTips = true
            };

            // 1. Bookmarks Handler
            _bookmarksHandler = new NavBarBookmarksHandler(
                _parentForm,
                _webViewController,
                _settingsManager,
                _isDarkModeProvider);

            // 2. Navigation Buttons Handler
            _navigationButtonsHandler = new NavBarNavigationButtonsHandler(
                _parentForm,
                _webViewController,
                _settingsManager,
                _webViewService,
                _isDarkModeProvider,
                () =>
                {
                    _bookmarksHandler.ToggleCurrentPageBookmark();
                    _bookmarksHandler.UpdateBookmarkButtonState();
                });
            _bookmarksHandler.BookmarkPageButton = _navigationButtonsHandler.BookmarkPageButton;

            // 3. Build Menus and Menu Items
            _menuItems = NavBarMenuBuilder.Build(
                _menuStrip,
                _parentForm,
                _webViewController,
                _settingsManager,
                _webViewService,
                _isDarkModeProvider,
                _bookmarksHandler.BookmarksMenu,
                OpenHistoryDialog,
                OpenDownloadsDialog,
                CheckForUpdatesManualAsync,
                _updateService,
                _backupCoordinator);

            // 4. Insert Navigation Buttons at 0..7
            _navigationButtonsHandler.InsertButtonsInto(_menuStrip);

            // 5. Update Handler
            _updateHandler = new NavBarUpdateHandler(
                _parentForm,
                _updateService,
                _settingsManager,
                _isDarkModeProvider,
                _downloadService,
                _packageVerifier,
                _menuStrip,
                _menuItems.HelpMenu,
                () => _menuItems.UpdatesMenuItem);

            // 6. Dismissal Handler (Low-level Win32 mouse hook & outside-click close)
            _dismissalHandler = new NavigationMenuDismissalHandler(
                _menuStrip,
                _parentForm.AsForm,
                () =>
                {
                    var webView = _webViewController.WebView;
                    if (webView != null && !webView.IsDisposed)
                    {
                        _parentForm.AsForm.ActiveControl = webView;
                        webView.Focus();
                    }
                });

            // 7. Text Handler
            _textHandler = new NavBarTextHandler(
                _parentForm,
                _settingsManager,
                _menuItems,
                _navigationButtonsHandler,
                _bookmarksHandler,
                _updateHandler,
                () =>
                {
                    Height = _menuStrip.PreferredSize.Height;
                    CenterMenuStrip();
                });

            // 8. Add MenuStrip to control
            Controls.Add(_menuStrip);
            Dock = DockStyle.Top;
            Height = _menuStrip.PreferredSize.Height;
            _menuStrip.Resize += (s, e) => CenterMenuStrip();
            CenterMenuStrip();

            // 9. Subscribe to WebView controller events
            _webViewController.NavigationStarting += (s, e) =>
            {
                UpdateNavButtons();
                UpdateBookmarkButtonState();
                if (!_parentForm.IsDashboardVisible)
                {
                    _navigationButtonsHandler.Omnibox?.UpdateCurrentUrl(e.Uri, false);
                }
            };

            _webViewController.NavigationCompleted += (s, e) =>
            {
                UpdateNavButtons();
                UpdateBookmarkButtonState();
            };

            _webViewController.InitializationCompleted += (s, e) =>
            {
                UpdateNavButtons();
                UpdateBookmarkButtonState();
            };

            _webViewController.PageClicked += (s, e) => CloseAllMenus();
            _navigationButtonsHandler.Omnibox.EditingStarted += (s, e) => CloseAllMenus();
        }

        #region Public API Forwarders

        public void UpdateBookmarkButtonState()
        {
            _bookmarksHandler.UpdateBookmarkButtonState();
        }

        public void UpdateNavButtons()
        {
            _navigationButtonsHandler.UpdateNavButtons();
        }

        public void ToggleCurrentPageBookmark()
        {
            _bookmarksHandler.ToggleCurrentPageBookmark();
        }

        public async Task CheckForUpdatesManualAsync()
        {
            await _updateHandler.CheckForUpdatesManualAsync();
        }

        public void ApplyDarkMode(bool isDark)
        {
            bool highContrast = SystemAccessibility.IsHighContrast;

            Color bg = highContrast
                ? SystemColors.Control
                : (isDark ? UIConstants.DarkMode.MenuStripBackColor : UIConstants.NavBarLightBg);

            BackColor = bg;
            _menuStrip.BackColor = bg;
            _menuStrip.ForeColor = highContrast
                ? SystemColors.ControlText
                : (isDark ? UIConstants.DarkMode.MenuStripForeColor : SystemColors.ControlText);
            _menuStrip.Renderer = isDark ? new DarkMenuRenderer() : new LightMenuRenderer();

            _navigationButtonsHandler.ApplyDarkModeToNavigationControls(isDark);
            _navigationButtonsHandler.UpdateThemeButtonState();
            CenterMenuStrip();
        }

        public void UpdateThemeButtonState()
        {
            _navigationButtonsHandler.UpdateThemeButtonState();
        }

        public void PerformHardRefresh()
        {
            _navigationButtonsHandler.PerformHardRefresh();
        }

        public void ApplyTexts()
        {
            _textHandler.ApplyTexts();
        }

        public void ApplySystemMenuTexts()
        {
            _textHandler.ApplySystemMenuTexts();
        }

        public void SetUpdateAvailable(bool available)
        {
            _updateHandler.SetUpdateAvailable(available);
        }

        public void RefreshBlinkState()
        {
            _updateHandler.RefreshBlinkState();
        }

        public void OpenHistoryDialog()
        {
            using var dialog = new HistoryDialog(
                _settingsManager,
                _isDarkModeProvider(),
                url => _parentForm.ShowWebView(url));
            dialog.ShowDialog(_parentForm.AsForm);
        }

        public void OpenDownloadsDialog()
        {
            using var dialog = new DownloadsDialog(
                _settingsManager,
                _isDarkModeProvider(),
                _wallpaperService,
                url => _parentForm.ShowWebView(url));
            dialog.ShowDialog(_parentForm.AsForm);
        }

        public void OpenManageBookmarksDialog()
        {
            _bookmarksHandler.OpenManageBookmarksDialog();
        }

        public void CloseAllMenus()
        {
            _dismissalHandler.CloseAllMenus();
        }

        public void RebuildBookmarksMenu()
        {
            _bookmarksHandler.RebuildBookmarksMenu();
        }

        #endregion

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CenterMenuStrip();
        }

        private void CenterMenuStrip()
        {
            if (_menuStrip == null || _menuStrip.IsDisposed) return;

            _navigationButtonsHandler?.AdjustOmniboxWidth(ClientSize.Width);
            int menuWidth = _menuStrip.PreferredSize.Width;
            int targetX = Math.Max(0, (ClientSize.Width - menuWidth) / 2);
            _menuStrip.Location = new Point(targetX, 0);
            Height = _menuStrip.PreferredSize.Height;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dismissalHandler?.Dispose();
                _updateHandler?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
