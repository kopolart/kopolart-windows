using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Forms;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls.NavBar
{
    /// <summary>
    /// Manages top-level navigation buttons: Back, Forward, Refresh, Search, Copy Link,
    /// Open in Browser, Bookmark, and Theme Toggle.
    /// </summary>
    public sealed class NavBarNavigationButtonsHandler
    {
        private readonly IMainFormActions _parentForm;
        private readonly WebViewController _webViewController;
        private readonly ISettingsManager _settingsManager;
        private readonly IWebViewService _webViewService;
        private readonly Func<bool> _isDarkModeProvider;
        private readonly Action _onToggleBookmark;

        public ToolStripButton DashboardButton { get; private set; } = null!;
        public ToolStripButton BackButton { get; private set; } = null!;
        public ToolStripButton ForwardButton { get; private set; } = null!;
        public ToolStripButton RefreshButton { get; private set; } = null!;
        public ToolStripControlHost OmniboxHost { get; private set; } = null!;
        public NavBarOmniboxControl Omnibox { get; private set; } = null!;
        public ToolStripButton SearchButton { get; private set; } = null!;
        public ToolStripButton CopyLinkButton { get; private set; } = null!;
        public ToolStripButton OpenInBrowserButton { get; private set; } = null!;
        public ToolStripButton BookmarkPageButton { get; private set; } = null!;
        public ToolStripButton ThemeToggleButton { get; private set; } = null!;
        public ToolStripSeparator NavSeparator { get; private set; } = null!;

        public NavBarNavigationButtonsHandler(
            IMainFormActions parentForm,
            WebViewController webViewController,
            ISettingsManager settingsManager,
            IWebViewService webViewService,
            Func<bool> isDarkModeProvider,
            Action onToggleBookmark)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _webViewController = webViewController ?? throw new ArgumentNullException(nameof(webViewController));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
            _onToggleBookmark = onToggleBookmark ?? throw new ArgumentNullException(nameof(onToggleBookmark));

            InitializeButtons();
        }

        private void InitializeButtons()
        {
            DashboardButton = new ToolStripButton
            {
                Name = "dashboardButton",
                Text = "\uE80F", // Segoe MDL2 Assets Home
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 0, 0),
                AutoToolTip = false
            };
            DashboardButton.Click += (s, e) =>
            {
                if (_parentForm.IsDashboardVisible)
                {
                    _parentForm.ShowWebView();
                }
                else
                {
                    _parentForm.ShowDashboard();
                }
            };

            BackButton = new ToolStripButton
            {
                Name = "backButton",
                Text = "\uE72B", // Segoe MDL2 Assets Left Arrow
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 0, 0),
                Enabled = false,
                AutoToolTip = false
            };
            BackButton.Click += (s, e) => _webViewController.GoBack();

            ForwardButton = new ToolStripButton
            {
                Name = "forwardButton",
                Text = "\uE72A", // Segoe MDL2 Assets Right Arrow
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 0, 0),
                Enabled = false,
                AutoToolTip = false
            };
            ForwardButton.Click += (s, e) => _webViewController.GoForward();

            RefreshButton = new ToolStripButton
            {
                Name = "refreshButton",
                Text = "\uE72C", // Segoe MDL2 Assets Refresh
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 5, 0),
                AutoToolTip = false
            };
            RefreshButton.Click += (s, e) => _webViewController.Reload();

            Omnibox = new NavBarOmniboxControl();

            OmniboxHost = new ToolStripControlHost(Omnibox)
            {
                Name = "omniboxHost",
                AutoSize = false,
                Size = Omnibox.Size,
                Margin = new Padding(6, 1, 6, 1)
            };

            SearchButton = new ToolStripButton
            {
                Name = "searchButton",
                Text = "\uE721", // Segoe MDL2 Assets Find / Search icon
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 0, 0),
                AutoToolTip = false
            };
            SearchButton.Click += (s, e) => _webViewController.ShowFindUI();

            CopyLinkButton = new ToolStripButton
            {
                Name = "copyLinkButton",
                Text = "\uE8C8", // Segoe MDL2 Assets Copy
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 0, 0),
                AutoToolTip = false
            };
            CopyLinkButton.Click += async (s, e) => await CopyCurrentUrlAsync();

            BookmarkPageButton = new ToolStripButton
            {
                Name = "bookmarkPageButton",
                Text = "\uE734", // Segoe MDL2 Assets Outline Star
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 5, 0),
                AutoToolTip = false
            };
            BookmarkPageButton.Click += (s, e) => _onToggleBookmark();

            OpenInBrowserButton = new ToolStripButton
            {
                Name = "openInBrowserButton",
                Text = "\uE8A7", // Segoe MDL2 Assets Open in external browser / Launch icon
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 0, 0),
                AutoToolTip = false
            };
            OpenInBrowserButton.Click += (s, e) =>
            {
                var url = _webViewController.WebView?.Source?.ToString();
                if (!string.IsNullOrWhiteSpace(url) && url != "about:blank")
                {
                    WebView2SecurityManager.TryOpenExternalUri(url);
                }
            };

            ThemeToggleButton = new ToolStripButton
            {
                Name = "themeToggleButton",
                Font = new Font("Segoe MDL2 Assets", 10),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 5, 0),
                AutoToolTip = false
            };
            ThemeToggleButton.Click += async (s, e) =>
            {
                await WebViewThemeSynchronizer.ToggleAppThemeAsync(_parentForm, _webViewController, _isDarkModeProvider);
            };

            NavSeparator = new ToolStripSeparator
            {
                Name = "navSeparator",
                Margin = new Padding(8, 4, 8, 4)
            };
        }

        public void InsertButtonsInto(MenuStrip menuStrip)
        {
            menuStrip.Items.Insert(0, DashboardButton);
            menuStrip.Items.Insert(1, BackButton);
            menuStrip.Items.Insert(2, ForwardButton);
            menuStrip.Items.Insert(3, RefreshButton);
            menuStrip.Items.Insert(4, OmniboxHost);
            menuStrip.Items.Insert(5, SearchButton);
            menuStrip.Items.Insert(6, CopyLinkButton);
            menuStrip.Items.Insert(7, OpenInBrowserButton);
            menuStrip.Items.Insert(8, BookmarkPageButton);
            menuStrip.Items.Insert(9, ThemeToggleButton);
            menuStrip.Items.Insert(10, NavSeparator);
        }

        public void UpdateNavButtons()
        {
            if (DashboardButton != null)
            {
                DashboardButton.Enabled = true;
            }

            if (_parentForm.IsDashboardVisible)
            {
                if (BackButton != null) BackButton.Enabled = false;
                if (ForwardButton != null) ForwardButton.Enabled = false;
                if (SearchButton != null) SearchButton.Enabled = false;
                if (CopyLinkButton != null) CopyLinkButton.Enabled = false;
                if (OpenInBrowserButton != null) OpenInBrowserButton.Enabled = false;
                if (BookmarkPageButton != null) BookmarkPageButton.Enabled = false;
                Omnibox?.UpdateCurrentUrl("https://kopolart.ir", true);
            }
            else
            {
                if (BackButton != null && ForwardButton != null)
                {
                    BackButton.Enabled = _webViewController.CanGoBack;
                    ForwardButton.Enabled = _webViewController.CanGoForward;
                }
                if (SearchButton != null) SearchButton.Enabled = true;
                if (CopyLinkButton != null) CopyLinkButton.Enabled = true;
                if (OpenInBrowserButton != null) OpenInBrowserButton.Enabled = true;
                if (BookmarkPageButton != null) BookmarkPageButton.Enabled = true;

                var currentUrl = _webViewController.WebView?.Source?.ToString();
                Omnibox?.UpdateCurrentUrl(currentUrl, false);
            }
        }

        public void UpdateThemeButtonState()
        {
            if (ThemeToggleButton == null) return;

            bool isDark = _isDarkModeProvider();

            if (isDark)
            {
                ThemeToggleButton.Text = "\uE706"; // Sun/Brightness icon
                ThemeToggleButton.ToolTipText = TextManager.GetString("TooltipToggleThemeLight");
            }
            else
            {
                ThemeToggleButton.Text = "\uE708"; // Contrast/Theme icon
                ThemeToggleButton.ToolTipText = TextManager.GetString("TooltipToggleThemeDark");
            }
        }

        public void ApplyDarkModeToNavigationControls(bool isDark)
        {
            var foreColor = isDark ? UIConstants.DarkMode.MenuStripForeColor : SystemColors.ControlText;

            DashboardButton.ForeColor = foreColor;
            BackButton.ForeColor = foreColor;
            ForwardButton.ForeColor = foreColor;
            RefreshButton.ForeColor = foreColor;
            SearchButton.ForeColor = foreColor;
            CopyLinkButton.ForeColor = foreColor;
            OpenInBrowserButton.ForeColor = foreColor;
            BookmarkPageButton.ForeColor = foreColor;
            ThemeToggleButton.ForeColor = foreColor;

            Omnibox?.ApplyDarkMode(isDark);
        }

        public void AdjustOmniboxWidth(int containerWidth)
        {
            if (Omnibox != null && OmniboxHost != null)
            {
                Omnibox.AdjustWidth(containerWidth);
                OmniboxHost.Size = Omnibox.Size;
            }
        }

        public async void PerformHardRefresh()
        {
            var wv = _webViewController.WebView;
            if (wv == null) return;

            try
            {
                await _webViewService.ClearCacheOnlyAsync(wv);

                if (_settingsManager.GetShowShortcutClearCachePrompt())
                {
                    CustomMessageBox.Show(
                        _parentForm,
                        TextManager.GetString("SettingsCacheCleared"),
                        TextManager.GetString("Success"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        _isDarkModeProvider());
                }

                _webViewController.Reload();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clear cache during hard refresh shortcut: {ex.Message}");
                _webViewController.Reload();
            }
        }

        private async Task CopyCurrentUrlAsync()
        {
            var url = _webViewController.WebView?.Source?.ToString();
            if (!string.IsNullOrWhiteSpace(url) && url != "about:blank")
            {
                try
                {
                    Clipboard.SetText(url);

                    CopyLinkButton.Text = "\uE73E"; // Checkmark icon
                    CopyLinkButton.ToolTipText = TextManager.GetString("LinkCopied");

                    await Task.Delay(1500);

                    if (CopyLinkButton.Owner != null && !CopyLinkButton.Owner.IsDisposed)
                    {
                        CopyLinkButton.Text = "\uE8C8"; // Copy icon
                        CopyLinkButton.ToolTipText = TextManager.GetString("CopyLinkTooltip");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to copy URL or update button state: {ex.Message}");
                }
            }
        }
    }
}

