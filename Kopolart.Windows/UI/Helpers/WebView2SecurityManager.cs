using Kopolart.Windows.Domain.Constants;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;

namespace Kopolart.Windows.UI.Helpers
{
    public static class WebView2SecurityManager
    {
        public static void ApplySecuritySettings(CoreWebView2 coreWebView2)
        {
            if (coreWebView2 == null)
            {
                throw new ArgumentNullException(nameof(coreWebView2));
            }

            try
            {
                // Enhanced security settings
                coreWebView2.Settings.IsZoomControlEnabled = false;
                // Disable context menus completely
                coreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                coreWebView2.Settings.AreDevToolsEnabled = false;
                coreWebView2.Settings.IsStatusBarEnabled = false;
                coreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                coreWebView2.Settings.IsWebMessageEnabled = true;
                coreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                coreWebView2.Settings.IsPinchZoomEnabled = false;

                // Additional security: Disable external file access
                coreWebView2.Settings.AreHostObjectsAllowed = false;

                Debug.WriteLine("WebView2 security settings applied successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply WebView2 security settings: {ex}");
                // Continue without these settings - app will still work
            }
        }

        public static bool IsValidNavigationUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return false;

            try
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var result))
                {
                    // Allow data: and about: schemes
                    if (result.Scheme.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                        result.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Allow the app's local offline page served via virtual host mapping
                    if (result.Host.Equals(AppConstants.Urls.OfflineVirtualHost, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Strict host validation
                    return result.Host.Equals(AppConstants.Urls.Domain, StringComparison.OrdinalIgnoreCase) ||
                           result.Host.EndsWith($".{AppConstants.Urls.Domain}", StringComparison.OrdinalIgnoreCase);
                }

                // Fallback for relative URIs or invalid formats (deny by default for security)
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating navigation URI: {ex}");
                return false;
            }
        }

        public static void TryOpenExternalUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri)) return;

            try
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var result))
                {
                    // Whitelist allowed schemes
                    if (result.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                        result.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                        result.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
                    {
                        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                    }
                    else
                    {
                        Debug.WriteLine($"Blocked external navigation to potentially unsafe scheme: {result.Scheme}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open external link: {ex}");
            }
        }
    }
}
