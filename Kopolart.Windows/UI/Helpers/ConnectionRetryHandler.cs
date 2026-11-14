using Kopolart.Windows.UI.Interfaces;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Encapsulates the connection retry logic for WebView2 navigation.
    /// Handles automatic retries on failure and provides callbacks for UI updates.
    /// 
    /// Two retry UIs exist by design:
    ///   - LoaderView (WinForms overlay): shown during initial load + max-retries-exhausted
    ///   - OfflinePageManager (virtual-host offline page): shown during post-load navigation failures
    /// This handler owns all EnableOfflinePage state transitions.
    /// </summary>
    public class ConnectionRetryHandler
    {
        private const int MaxRetries = 3;
        private int _currentRetryCount;
        private bool _isInitialLoad = true;

        public bool IsInitialLoad => _isInitialLoad;

        /// <summary>Called when the loader should show a loading/status message.</summary>
        public Action<string>? OnSetLoading { get; set; }

        /// <summary>Called when the loader should show an error with a retry button.</summary>
        public Action<string, string, Action>? OnShowError { get; set; }

        /// <summary>Called when navigation succeeded and the loader should hide.</summary>
        public Func<Task>? OnStopLoading { get; set; }

        /// <summary>Called when the loader should start its animation.</summary>
        public Action? OnStartLoading { get; set; }

        /// <summary>
        /// Handles a navigation completion event. Decides whether to retry, show error, or pass through.
        /// </summary>
        public async Task HandleNavigationCompletedAsync(
            INavigationController controller,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _isInitialLoad = false;
                _currentRetryCount = 0;
                controller.EnableOfflinePage = true;
                if (OnStopLoading != null)
                    await OnStopLoading();
                controller.WebView?.Focus();
            }
            else
            {
                if (_isInitialLoad)
                {
                    if (_currentRetryCount < MaxRetries)
                    {
                        _currentRetryCount++;
                        int delaySeconds = (int)Math.Pow(2, _currentRetryCount);
                        string retryMsg = string.Format(TextManager.GetString("LoaderRetryAttempt"), delaySeconds, _currentRetryCount, MaxRetries);
                        OnSetLoading?.Invoke(retryMsg);

                        await Task.Delay(delaySeconds * 1000);
                        controller.NavigateToOrigin();
                    }
                    else
                    {
                        controller.EnableOfflinePage = true;
                        string errorMsg = TextManager.GetString("LoaderConnectionFailed");
                        string retryBtn = TextManager.GetString("LoaderRetry");
                        OnShowError?.Invoke(
                            errorMsg,
                            retryBtn,
                            () =>
                            {
                                _currentRetryCount = 0;
                                _isInitialLoad = true;
                                controller.EnableOfflinePage = false;
                                string connMsg = TextManager.GetString("LoaderConnecting");
                                OnSetLoading?.Invoke(connMsg);
                                controller.NavigateToOrigin();
                            });
                    }
                }
                else
                {
                    // Post-load nav failure - let WebViewController show the inline offline page
                    if (OnStopLoading != null)
                        await OnStopLoading();
                }
            }
        }

        /// <summary>
        /// Handles a retry connection request (e.g., from the offline page "Try Again" button).
        /// Resets retry state, disables the offline page, shows loader, and navigates to origin.
        /// </summary>
        public void HandleRetryConnection(INavigationController controller)
        {
            _isInitialLoad = true;
            _currentRetryCount = 0;
            controller.EnableOfflinePage = false;
            OnStartLoading?.Invoke();
            controller.NavigateToOrigin();
        }
    }
}


