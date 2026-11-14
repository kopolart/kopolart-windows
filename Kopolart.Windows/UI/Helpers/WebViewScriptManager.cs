using Kopolart.Windows.Domain.Constants;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    public static class WebViewScriptManager
    {
        public const string DarkModeMonitorScript = @"
(function() {
    var lastTheme = null;

    function isSiteDark() {
        var doc = document.documentElement;
        if (doc.getAttribute('data-theme') === 'dark') return true;
        if (doc.getAttribute('data-theme-mode') === 'dark') return true;
        if (doc.getAttribute('data-theme-mode') === 'light') return false;
        var match = document.cookie.match(/(?:^|;\s*)darkmode-kopolart=([^;]+)/);
        var mode = match ? match[1] : 'auto';
        if (mode === 'dark') return true;
        if (mode === 'light') return false;
        return !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
    }

    function syncThemeCss(isDark) {
        var link = document.getElementById('dark-theme-css');
        if (link) {
            if (isDark && link.hasAttribute('media')) {
                link.removeAttribute('media');
            } else if (!isDark && !link.hasAttribute('media')) {
                link.setAttribute('media', 'not all');
            }
        }
    }

    function checkTheme() {
        var isDark = isSiteDark();
        syncThemeCss(isDark);

        if (isDark !== lastTheme) {
            lastTheme = isDark;
            try {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage('THEME_CHANGED:' + (isDark ? 'dark' : 'light'));
                }
            } catch (e) {}
        }
    }

    if (window.MutationObserver) {
        var observer = new MutationObserver(function() {
            checkTheme();
        });
        observer.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['class', 'data-theme', 'data-theme-mode']
        });
    }

    if (window.matchMedia) {
        var media = window.matchMedia('(prefers-color-scheme: dark)');
        if (media.addEventListener) {
            media.addEventListener('change', checkTheme);
        } else if (media.addListener) {
            media.addListener(checkTheme);
        }
    }

    document.addEventListener('click', function(e) {
        if (e.target && (e.target.classList.contains('dark-mode-btn') || e.target.closest('.dark-mode-btn'))) {
            setTimeout(checkTheme, 50);
            setTimeout(checkTheme, 200);
            setTimeout(checkTheme, 500);
        }
    }, true);

    setInterval(checkTheme, 800);

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', checkTheme);
    } else {
        checkTheme();
    }

    document.addEventListener('visibilitychange', function() {
        if (!document.hidden) checkTheme();
    });
})();
";

        public static async Task InjectInitialScriptsAsync(CoreWebView2 coreWebView2)
        {
            if (coreWebView2 == null) return;

            try
            {
                await coreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(DarkModeMonitorScript);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to inject dark mode monitor script: {ex.Message}");
            }

            try
            {
                await coreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    EmbeddedTextAssets.Get(EmbeddedTextAssets.ShortcutForwarderScript));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to inject shortcut forwarding script: {ex.Message}");
            }
        }

        public static void InjectShortcutForwarder(CoreWebView2? coreWebView2)
        {
            if (coreWebView2 == null) return;

            try
            {
                _ = coreWebView2.ExecuteScriptAsync(
                    EmbeddedTextAssets.Get(EmbeddedTextAssets.ShortcutForwarderScript));
            }
            catch { /* non-critical */ }
        }

        public static async Task ShowFindUIAsync(CoreWebView2? coreWebView2, bool isDarkMode)
        {
            if (coreWebView2 == null) return;

            try
            {
                var config = new
                {
                    isDark = isDarkMode,
                    isRtl = true,
                    placeholder = TextManager.GetString("SearchInPage"),
                    nextText = TextManager.GetString("Next"),
                    prevText = TextManager.GetString("Previous"),
                    css = EmbeddedTextAssets.Get(EmbeddedTextAssets.FindBarStyles)
                };

                string script =
                    $"window.__kopolartFindBarConfig = {JsonSerializer.Serialize(config)};\n" +
                    EmbeddedTextAssets.Get(EmbeddedTextAssets.FindBarScript);

                await coreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to trigger find in page: {ex.Message}");
            }
        }

        public static bool TryParseShortcutMessage(string payload, out Keys keyData)
        {
            keyData = Keys.None;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                var code = root.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
                if (string.IsNullOrEmpty(code))
                    return false;

                keyData = MapDomCodeToKey(code);
                if (keyData == Keys.None)
                    return false;

                if (root.TryGetProperty("ctrl", out var ctrl) && ctrl.GetBoolean()) keyData |= Keys.Control;
                if (root.TryGetProperty("shift", out var shift) && shift.GetBoolean()) keyData |= Keys.Shift;
                if (root.TryGetProperty("alt", out var alt) && alt.GetBoolean()) keyData |= Keys.Alt;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to handle shortcut message: {ex.Message}");
                return false;
            }
        }

        public static Keys MapDomCodeToKey(string code)
        {
            return code switch
            {
                "F5" => Keys.F5,
                "Escape" => Keys.Escape,
                "KeyR" => Keys.R,
                "KeyF" => Keys.F,
                "KeyD" => Keys.D,
                "KeyH" => Keys.H,
                "ArrowLeft" => Keys.Left,
                "ArrowRight" => Keys.Right,
                _ => Keys.None
            };
        }

        public static bool IsTrustedMessageSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
                return false;

            if (sourceUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                sourceUri.Host.Equals(AppConstants.Urls.OfflineVirtualHost, StringComparison.OrdinalIgnoreCase))
                return true;

            return sourceUri.Host.Equals(AppConstants.Urls.Domain, StringComparison.OrdinalIgnoreCase) ||
                   sourceUri.Host.EndsWith($".{AppConstants.Urls.Domain}", StringComparison.OrdinalIgnoreCase);
        }
    }
}

