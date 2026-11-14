using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Synchronizes dark mode cookies and DOM styling into WebView2.
    /// </summary>
    public static class WebViewThemeSynchronizer
    {
        public static async Task ToggleAppThemeAsync(
            IMainFormActions parentForm,
            WebViewController webViewController,
            Func<bool> isDarkModeProvider)
        {
            bool isDark = isDarkModeProvider();
            bool newDarkMode = !isDark;

            // Update parent theme states
            parentForm.UpdateThemeState(newDarkMode);

            // Set cookie in WebView2 and apply theme to DOM/site
            await SyncDarkModeToWebViewAsync(webViewController, newDarkMode);

            // If offline page is showing, reload it to reflect new theme query parameters
            if (webViewController.IsShowingOfflinePage)
            {
                webViewController.Reload();
            }
        }

        public static async Task SyncDarkModeToWebViewAsync(WebViewController webViewController, bool isDark)
        {
            try
            {
                var coreWv2 = webViewController.WebView?.CoreWebView2;
                if (coreWv2 == null) return;

                var cookieManager = coreWv2.CookieManager;
                if (cookieManager != null)
                {
                    string cookieValue = isDark ? UIConstants.DarkMode.CookieValue : "light";
                    string currentUrl = webViewController.WebView?.Source?.ToString() ?? AppConstants.Urls.MainUrl;
                    bool isSecure = currentUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase);

                    // Get all cookies for the current URL to find existing ones
                    var cookies = await cookieManager.GetCookiesAsync(currentUrl);
                    bool found = false;

                    foreach (var cookie in cookies)
                    {
                        if (cookie.Name == UIConstants.DarkMode.CookieName)
                        {
                            cookie.Value = cookieValue;
                            cookie.IsSecure = isSecure;
                            cookieManager.AddOrUpdateCookie(cookie);
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        // If no cookie exists, create one for the current domain/host
                        string domain = "kopolart.ir";
                        if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var uri))
                        {
                            domain = uri.Host;
                        }

                        var cookie = cookieManager.CreateCookie(
                            UIConstants.DarkMode.CookieName,
                            cookieValue,
                            domain,
                            "/");
                        cookie.Expires = DateTime.UtcNow.AddYears(1);
                        cookie.IsSecure = isSecure;
                        cookieManager.AddOrUpdateCookie(cookie);
                    }
                }

                // Run JS script to set cookie in current document and update DOM theme
                try
                {
                    string isDarkJs = isDark ? "true" : "false";
                    string cookieValJs = isDark ? UIConstants.DarkMode.CookieValue : "light";
                    string jsScript = $@"
(function() {{
    var isDark = {isDarkJs};
    var cookieName = '{UIConstants.DarkMode.CookieName}';
    var cookieValue = '{cookieValJs}';
    
    // 1. Set document cookie
    document.cookie = cookieName + '=' + cookieValue + '; path=/; max-age=31536000; SameSite=Lax' + (location.protocol === 'https:' ? '; secure' : '');

    // 2. Enable or disable dark theme stylesheet
    var darkLink = document.getElementById('dark-theme-css');
    if (darkLink) {{
        if (isDark) {{
            darkLink.removeAttribute('media');
        }} else {{
            darkLink.setAttribute('media', 'not all');
        }}
    }}

    // 3. Update HTML data-theme and data-theme-mode
    var doc = document.documentElement;
    doc.setAttribute('data-theme-mode', isDark ? 'dark' : 'light');
    if (isDark) {{
        doc.classList.add('dark');
        doc.setAttribute('data-theme', 'dark');
    }} else {{
        doc.classList.remove('dark');
        doc.removeAttribute('data-theme');
    }}

    // 4. Update meta theme-color
    var meta = document.querySelector('meta[name=""theme-color""]') || document.getElementById('theme-color-meta');
    if (meta) {{
        meta.setAttribute('content', isDark ? '#403e3b' : '#e2cdb4');
    }}

    // 5. Update website dark mode toggle button icon and state
    var btn = document.querySelector('.dark-mode-btn');
    if (btn) {{
        btn.classList.toggle('active', isDark);
        btn.classList.toggle('ic-sun', isDark);
        btn.classList.toggle('ic-moon', !isDark);
        btn.classList.remove('ic-theme-auto', 'theme-auto');
        btn.setAttribute('aria-pressed', isDark ? 'true' : 'false');
    }}

    // 6. Update website logo
    var logo = document.querySelector('.menu-logo img');
    if (logo) {{
        logo.src = isDark ? '/img/logo-menu-dark.webp' : '/img/logo-menu-light.webp';
    }}

    // 7. Dispatch events for any page listeners
    window.dispatchEvent(new CustomEvent('themechange', {{ detail: cookieValue }}));
}})();";
                    await coreWv2.ExecuteScriptAsync(jsScript);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to execute cookie JS: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting dark mode cookie: {ex.Message}");
            }
        }
    }
}
