using Kopolart.Windows.UI.Controllers;
using System;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Handles keyboard shortcuts for the main form.
    /// </summary>
    public static class KeyboardShortcutHandler
    {
        /// <param name="keyData">The key combination pressed.</param>
        /// <param name="controller">The WebView controller to act on.</param>
        /// <param name="onBackOrExit">Called when Escape is pressed (go back or prompt exit).</param>
        /// <param name="onBookmarkCurrentPage">Called when Ctrl+D is pressed.</param>
        /// <param name="onHardRefresh">Called when Ctrl+Shift+R is pressed.</param>
        /// <param name="onShowHistory">Called when Ctrl+H is pressed.</param>
        /// <param name="onShowDownloads">Called when Ctrl+J is pressed.</param>
        /// <param name="onShowDashboard">Called when Alt+Home is pressed to toggle/show dashboard.</param>
        /// <returns>True if the key was handled.</returns>
        public static bool ProcessKey(Keys keyData, WebViewController controller, Action onBackOrExit, Action? onBookmarkCurrentPage = null, Action? onHardRefresh = null, Action? onShowHistory = null, Action? onShowDownloads = null, Action? onShowDashboard = null)
        {
            // Alt+Home to Toggle / Show Dashboard
            if (keyData == (Keys.Alt | Keys.Home))
            {
                onShowDashboard?.Invoke();
                return true;
            }

            if (controller.WebView is { CoreWebView2: not null })
            {
                // Ctrl+Shift+R to Clear Cache and Reload (Hard Refresh)
                if (keyData == (Keys.Control | Keys.Shift | Keys.R))
                {
                    if (onHardRefresh != null)
                    {
                        onHardRefresh();
                    }
                    else
                    {
                        _ = controller.ClearCacheAndReloadAsync();
                    }
                    return true;
                }

                // F5 or Ctrl+R to Refresh
                if (keyData is Keys.F5 or (Keys.Control | Keys.R))
                {
                    controller.Reload();
                    return true;
                }

                // Alt+Left to Go Back
                if (keyData == (Keys.Alt | Keys.Left))
                {
                    if (controller.CanGoBack)
                    {
                        controller.GoBack();
                        return true;
                    }
                }

                // Alt+Right to Go Forward
                if (keyData == (Keys.Alt | Keys.Right))
                {
                    if (controller.CanGoForward)
                    {
                        controller.GoForward();
                        return true;
                    }
                }

                // Ctrl+F to Find in Page
                if (keyData == (Keys.Control | Keys.F))
                {
                    controller.ShowFindUI();
                    return true;
                }

                // Ctrl+D to Bookmark Current Page
                if (keyData == (Keys.Control | Keys.D))
                {
                    onBookmarkCurrentPage?.Invoke();
                    return true;
                }

                // Ctrl+H to Open History Dialog
                if (keyData == (Keys.Control | Keys.H))
                {
                    onShowHistory?.Invoke();
                    return true;
                }

                // Ctrl+J to Open Downloads Dialog
                if (keyData == (Keys.Control | Keys.J))
                {
                    onShowDownloads?.Invoke();
                    return true;
                }
            }

            if (keyData == Keys.Escape)
            {
                onBackOrExit();
                return true;
            }

            return false;
        }
    }
}
