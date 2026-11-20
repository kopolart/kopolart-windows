using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Coordinates theme application (light vs dark mode) across the main form,
    /// child controls, WebView2 background, and web content synchronization.
    /// </summary>
    public class FormThemeCoordinator
    {
        private readonly Form _form;
        private readonly Panel _contentPanel;
        private readonly LoaderView _loaderView;
        private readonly WebViewController _webViewController;
        private readonly ISettingsManager _settingsManager;
        private readonly Func<NavigationLoadingOverlay?> _navOverlayProvider;
        private readonly Func<AppNavigationBar?> _navBarProvider;
        private readonly Func<DashboardView?>? _dashboardViewProvider;

        private bool _isDarkMode;

        public bool IsDarkMode => _isDarkMode;

        public FormThemeCoordinator(
            Form form,
            Panel contentPanel,
            LoaderView loaderView,
            WebViewController webViewController,
            ISettingsManager settingsManager,
            Func<NavigationLoadingOverlay?> navOverlayProvider,
            Func<AppNavigationBar?> navBarProvider,
            Func<DashboardView?>? dashboardViewProvider = null)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _contentPanel = contentPanel ?? throw new ArgumentNullException(nameof(contentPanel));
            _loaderView = loaderView ?? throw new ArgumentNullException(nameof(loaderView));
            _webViewController = webViewController ?? throw new ArgumentNullException(nameof(webViewController));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _navOverlayProvider = navOverlayProvider ?? throw new ArgumentNullException(nameof(navOverlayProvider));
            _navBarProvider = navBarProvider ?? throw new ArgumentNullException(nameof(navBarProvider));
            _dashboardViewProvider = dashboardViewProvider;

            _isDarkMode = _settingsManager.GetIsDarkMode();
        }

        public void UpdateThemeState(bool isDark)
        {
            _isDarkMode = isDark;
            _settingsManager.SetIsDarkMode(isDark);
            ApplyDarkModeToUI(isDark);
            _ = ApplyAppThemeToWebContentAsync();
        }

        public void ApplyDarkModeToUI(bool isDark)
        {
            HighContrastTheme.Restore(_form);

            if (_form.IsHandleCreated)
            {
                NativeMethods.SetImmersiveDarkMode(_form.Handle, isDark);
            }

            _webViewController.IsDarkMode = isDark;
            _webViewController.ApplyAccessibilityZoom();
            _navOverlayProvider()?.ApplyDarkMode(isDark);
            _loaderView.ApplyDarkMode(isDark);
            _navBarProvider()?.ApplyDarkMode(isDark);
            _dashboardViewProvider?.Invoke()?.ApplyTheme(isDark);

            if (_webViewController.WebView != null)
            {
                _webViewController.WebView.DefaultBackgroundColor = isDark
                    ? UIConstants.DarkMode.BrandBackground
                    : UIConstants.BrandBackground;
            }

            if (isDark)
            {
                _contentPanel.BackColor = UIConstants.DarkMode.BrandBackground;
                _form.BackColor = UIConstants.DarkMode.BrandBackground;
            }
            else
            {
                _contentPanel.BackColor = UIConstants.BrandBackground;
                _form.BackColor = UIConstants.BrandBackground;
            }

            _webViewController.UpdateLayoutBounds();

            HighContrastTheme.Apply(_form);
        }

        /// <summary>
        /// Pushes the app theme into the web content. The desktop app owns the theme
        /// preference, so the website follows it instead of the other way around.
        /// </summary>
        public async Task ApplyAppThemeToWebContentAsync()
        {
            try
            {
                if (_webViewController.WebView?.CoreWebView2 == null)
                    return;

                await WebViewThemeSynchronizer.SyncDarkModeToWebViewAsync(_webViewController, _isDarkMode);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply app theme to web content: {ex}");
            }
        }

        /// <summary>
        /// The website can toggle its own styling, but it must not overwrite the
        /// app-wide theme preference that the user selected in settings.
        /// </summary>
        public void HandleThemeChanged(bool isDark)
        {
            Debug.WriteLine($"[Theme] Ignoring web theme change (app is {(_isDarkMode ? "dark" : "light")}).");

            // Re-assert the app theme so the page cannot leave the UI out of sync.
            _ = ApplyAppThemeToWebContentAsync();
        }
    }
}
