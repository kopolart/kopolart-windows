using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controllers
{
    public class WebViewController(
        IWebViewService webViewService,
        ISettingsManager settingsManager,
        IAppVisitTrackerService? appVisitTrackerService = null) : INavigationController, IDisposable
    {
        private readonly IWebViewService _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
        private readonly ISettingsManager _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        private readonly IAppVisitTrackerService? _appVisitTrackerService = appVisitTrackerService;
        private readonly WebViewOfflineCoordinator _offlineCoordinator = new();

        private WebView2? _webView;
        private bool _disposed;
        private bool _isInitialized;
        private bool _coreEventsAttached;

        public event EventHandler<CoreWebView2NavigationStartingEventArgs>? NavigationStarting;
        public event EventHandler<CoreWebView2NavigationCompletedEventArgs>? NavigationCompleted;
        public event EventHandler<CoreWebView2ProcessFailedEventArgs>? ProcessFailed;
        public event EventHandler? InitializationCompleted;
        public event EventHandler<Exception?>? InitializationFailed;
        public event EventHandler? RetryConnection;
        public event EventHandler? DocumentTitleChanged;
        public event EventHandler<bool>? ThemeChanged;
        public event EventHandler<CoreWebView2DownloadStartingEventArgs>? DownloadStarting;
        public event EventHandler<long>? BytesReceived;
        public event EventHandler? PageClicked;

        public Func<Keys, bool>? AcceleratorKeyHandler { get; set; }
        public WebView2? WebView => _webView;
        public IAppVisitTrackerService? AppVisitTrackerService => _appVisitTrackerService;

        public bool EnableOfflinePage
        {
            get => _offlineCoordinator.EnableOfflinePage;
            set => _offlineCoordinator.EnableOfflinePage = value;
        }

        public bool IsShowingOfflinePage => _offlineCoordinator.IsShowingOfflinePage;
        public bool IsDarkMode { get; set; }

        public string DocumentTitle => _offlineCoordinator.GetDocumentTitle(_webView?.CoreWebView2);
        public bool CanGoBack => _webView?.CanGoBack ?? false;
        public bool CanGoForward => _webView?.CanGoForward ?? false;

        public async Task<WebView2> InitializeAsync(Control parentControl)
        {
            return await InitializeAsync(parentControl, CancellationToken.None);
        }

        public async Task<WebView2> InitializeAsync(Control parentControl, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (parentControl == null)
                throw new ArgumentNullException(nameof(parentControl));

            if (parentControl.IsDisposed)
                throw new ObjectDisposedException(nameof(parentControl));

            EnsureUiThread(parentControl);

            if (_webView == null || _webView.IsDisposed)
            {
                _webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };

                _webView.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
                _webView.NavigationCompleted += OnNavigationCompleted;
                _webView.NavigationStarting += OnNavigationStarting;

                parentControl.Controls.Add(_webView);
                parentControl.Resize += OnParentControlResize;
                UpdateLayoutBounds();
            }
            else if (_webView.Parent != parentControl)
            {
                if (_webView.Parent != null)
                {
                    _webView.Parent.Resize -= OnParentControlResize;
                }
                _webView.Parent?.Controls.Remove(_webView);
                parentControl.Controls.Add(_webView);
                parentControl.Resize += OnParentControlResize;
                _webView.Dock = DockStyle.Fill;
                UpdateLayoutBounds();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!_isInitialized)
            {
                await _webViewService.InitializeAsync(_webView).ConfigureAwait(true);
                await EnsureCoreEventsAndScriptsAsync().ConfigureAwait(true);

                cancellationToken.ThrowIfCancellationRequested();

                _isInitialized = true;
                _offlineCoordinator.IsShowingOfflinePage = false;

                string startUrl = AppConstants.Urls.MainUrl;
                if (_settingsManager.GetRestoreLastVisitedPage())
                {
                    var lastUrl = _settingsManager.GetLastVisitedUrl();
                    if (!string.IsNullOrWhiteSpace(lastUrl) && WebView2SecurityManager.IsValidNavigationUri(lastUrl))
                    {
                        startUrl = lastUrl;
                    }
                }

                if (_webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.Navigate(startUrl);
                }
                else if (Uri.TryCreate(startUrl, UriKind.Absolute, out var uri))
                {
                    _webView.Source = uri;
                }
                else
                {
                    _webView.Source = CreateMainUri();
                }
            }

            return _webView;
        }

        public async Task InstallRuntimeAsync(IProgress<int> progress, Action<string> statusCallback)
        {
            await InstallRuntimeAsync(progress, statusCallback, CancellationToken.None);
        }

        public async Task InstallRuntimeAsync(IProgress<int> progress, Action<string> statusCallback, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            await _webViewService.InstallRuntimeAsync(progress, statusCallback).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
        }

        public void DisposeWebView()
        {
            if (_webView == null)
                return;

            try
            {
                DetachEvents(_webView);

                if (_webView.Parent != null && !_webView.Parent.IsDisposed)
                {
                    _webView.Parent.Resize -= OnParentControlResize;
                    _webView.Parent.Controls.Remove(_webView);
                }

                _webView.Dispose();
            }
            finally
            {
                _webView = null;
                _isInitialized = false;
                _coreEventsAttached = false;
                _offlineCoordinator.Reset();
            }
        }

        public void GoBack()
        {
            ThrowIfDisposed();

            if (_webView is { CoreWebView2: not null, CanGoBack: true })
            {
                _webView.GoBack();
            }
        }

        public void GoForward()
        {
            ThrowIfDisposed();

            if (_webView is { CoreWebView2: not null, CanGoForward: true })
            {
                _webView.GoForward();
            }
        }

        public void Reload()
        {
            ThrowIfDisposed();

            if (_webView == null)
                return;

            if (_offlineCoordinator.IsShowingOfflinePage)
            {
                NavigateToOrigin();
                return;
            }

            _webView.Reload();
        }

        public async Task ClearCacheAndReloadAsync()
        {
            ThrowIfDisposed();
            if (_webView != null)
            {
                try
                {
                    await _webViewService.ClearCacheOnlyAsync(_webView);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to clear cache during hard reload: {ex.Message}");
                }
                Reload();
            }
        }

        public void NavigateToOrigin()
        {
            ThrowIfDisposed();

            if (_webView?.CoreWebView2 == null)
                return;

            _offlineCoordinator.IsShowingOfflinePage = false;
            _webView.CoreWebView2.Navigate(AppConstants.Urls.MainUrl);
        }

        public void ShowFindUI()
        {
            ThrowIfDisposed();
            _ = WebViewScriptManager.ShowFindUIAsync(_webView?.CoreWebView2, IsDarkMode);
        }

        private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!WebView2SecurityManager.IsValidNavigationUri(e.Uri))
            {
                e.Cancel = true;
                WebView2SecurityManager.TryOpenExternalUri(e.Uri);
                return;
            }

            if (OfflinePageManager.IsOfflinePageUri(e.Uri))
            {
                _offlineCoordinator.OfflineNavigationId = e.NavigationId;
                _offlineCoordinator.IsShowingOfflinePage = true;
                return;
            }

            _offlineCoordinator.IsShowingOfflinePage = false;
            NavigationStarting?.Invoke(this, e);
        }

        private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_offlineCoordinator.OfflineNavigationId.HasValue && e.NavigationId == _offlineCoordinator.OfflineNavigationId.Value)
            {
                _offlineCoordinator.OfflineNavigationId = null;
                DocumentTitleChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (!e.IsSuccess && _offlineCoordinator.EnableOfflinePage && WebViewOfflineCoordinator.IsOfflineRelatedError(e.WebErrorStatus))
            {
                _offlineCoordinator.ShowOfflinePage(_webView?.CoreWebView2, IsDarkMode);
                DocumentTitleChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (e.IsSuccess)
            {
                _offlineCoordinator.IsShowingOfflinePage = false;
                var currentUrl = _webView?.Source?.ToString();
                if (!string.IsNullOrWhiteSpace(currentUrl))
                {
                    _settingsManager.AddToHistory(DocumentTitle, currentUrl);
                    _ = _appVisitTrackerService?.TrackBrowseVisitAsync(_webView, currentUrl);
                }

                WebViewScriptManager.InjectShortcutForwarder(_webView?.CoreWebView2);
            }

            NavigationCompleted?.Invoke(this, e);
        }

        private async void OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            try
            {
                if (!e.IsSuccess || _webView?.CoreWebView2 == null)
                {
                    Debug.WriteLine($"CoreWebView2 initialization failed: {e.InitializationException}");
                    InitializationFailed?.Invoke(this, e.InitializationException);
                    return;
                }

                await EnsureCoreEventsAndScriptsAsync().ConfigureAwait(true);
                InitializationCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during WebView2 initialization completed: {ex.Message}");
            }
        }

        private async Task EnsureCoreEventsAndScriptsAsync()
        {
            if (_coreEventsAttached || _webView?.CoreWebView2 == null)
                return;

            _coreEventsAttached = true;

            _webView.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
            _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
            _webView.CoreWebView2.ProcessFailed += OnProcessFailed;
            _webView.WebMessageReceived += OnWebMessageReceived;
            _webView.KeyDown += OnWebViewKeyDown;

            try
            {
                WebView2SecurityManager.ApplySecuritySettings(_webView.CoreWebView2);
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.ZoomFactor = 1.0;
                _webView.ZoomFactorChanged += (s, e) =>
                {
                    if (_webView.ZoomFactor != 1.0)
                    {
                        _webView.ZoomFactor = 1.0;
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply security settings: {ex.Message}");
            }

            _webView.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;
            _webView.CoreWebView2.DownloadStarting += OnDownloadStarting;
            _webView.CoreWebView2.WebResourceResponseReceived += OnWebResourceResponseReceived;

            _ = UpdateUserAgentAsync(_settingsManager.GetLayoutMode());

            try
            {
                await WebViewScriptManager.InjectInitialScriptsAsync(_webView.CoreWebView2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to inject initial scripts: {ex.Message}");
            }
        }

        private void OnContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs args)
        {
            args.Handled = true;
        }

        private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs args)
        {
            DownloadStarting?.Invoke(this, args);
        }

        private void OnWebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
        {
            try
            {
                if (args.Response?.Headers != null && args.Response.Headers.Contains("Content-Length"))
                {
                    var clStr = args.Response.Headers.GetHeader("Content-Length");
                    if (long.TryParse(clStr, out var cl) && cl > 0)
                    {
                        BytesReceived?.Invoke(this, cl);
                        return;
                    }
                }
                BytesReceived?.Invoke(this, 512);
            }
            catch { /* non-critical */ }
        }

        private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            Debug.WriteLine($"WebView2 ProcessFailed: Kind={e.ProcessFailedKind}, Reason={e.Reason}");
            ProcessFailed?.Invoke(this, e);
        }

        private void OnDocumentTitleChanged(object? sender, object e)
        {
            DocumentTitleChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnWebViewKeyDown(object? sender, KeyEventArgs e)
        {
            if (AcceleratorKeyHandler == null)
                return;

            if (AcceleratorKeyHandler(e.KeyData))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string? message = null;

            try
            {
                message = e.TryGetWebMessageAsString();
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Failed to read web message as string: {ex.Message}");
            }

            if (message == "KP_PAGE_CLICK")
            {
                PageClicked?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (!WebViewScriptManager.IsTrustedMessageSource(e.Source))
                return;

            if (message == AppConstants.Messages.RetryConnection)
            {
                RetryConnection?.Invoke(this, EventArgs.Empty);
            }
            else if (message != null && message.StartsWith("THEME_CHANGED:"))
            {
                var theme = message.Substring("THEME_CHANGED:".Length);
                ThemeChanged?.Invoke(this, theme == "dark");
            }
            else if (message != null && message.StartsWith("KP_SHORTCUT:"))
            {
                if (WebViewScriptManager.TryParseShortcutMessage(message.Substring("KP_SHORTCUT:".Length), out var keyData))
                {
                    AcceleratorKeyHandler?.Invoke(keyData);
                }
            }
        }

        private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;

            if (string.IsNullOrWhiteSpace(e.Uri))
                return;

            if (WebView2SecurityManager.IsValidNavigationUri(e.Uri))
            {
                _webView?.CoreWebView2?.Navigate(e.Uri);
            }
            else
            {
                WebView2SecurityManager.TryOpenExternalUri(e.Uri);
            }
        }

        private void DetachEvents(WebView2 webView)
        {
            webView.CoreWebView2InitializationCompleted -= OnCoreWebView2InitializationCompleted;
            webView.NavigationCompleted -= OnNavigationCompleted;
            webView.NavigationStarting -= OnNavigationStarting;
            webView.WebMessageReceived -= OnWebMessageReceived;
            webView.KeyDown -= OnWebViewKeyDown;

            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.DocumentTitleChanged -= OnDocumentTitleChanged;
                webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
                webView.CoreWebView2.ProcessFailed -= OnProcessFailed;
                webView.CoreWebView2.ContextMenuRequested -= OnContextMenuRequested;
                webView.CoreWebView2.DownloadStarting -= OnDownloadStarting;
                webView.CoreWebView2.WebResourceResponseReceived -= OnWebResourceResponseReceived;
            }

            _coreEventsAttached = false;
        }

        private static Uri CreateMainUri()
        {
            if (!Uri.TryCreate(AppConstants.Urls.MainUrl, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Invalid main URL: {AppConstants.Urls.MainUrl}");

            return uri;
        }

        private static void EnsureUiThread(Control control)
        {
            if (control.InvokeRequired)
                throw new InvalidOperationException("WebViewController must be used on the UI thread.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WebViewController));
        }

        public async Task UpdateUserAgentAsync(string mode)
        {
            if (_webView?.CoreWebView2 == null) return;
            await WebViewDeviceEmulator.UpdateDeviceEmulationAsync(_webView.CoreWebView2, mode);
            UpdateLayoutBounds();
        }

        private void OnParentControlResize(object? sender, EventArgs e)
        {
            UpdateLayoutBounds();
        }

        public void UpdateLayoutBounds()
        {
            WebViewDeviceEmulator.UpdateLayoutBounds(_webView, _settingsManager.GetLayoutMode());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                DisposeWebView();
            }

            _disposed = true;
        }
    }
}
