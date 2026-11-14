using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class AppVisitTrackerService : IAppVisitTrackerService
    {
        private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(10);
        private static readonly System.Net.Http.HttpClient TrackingHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _lastTrackedMap = new();
        private DateTime _lastTrackedUtc = DateTime.MinValue;
        private readonly Lock _lock = new();

        public async Task TrackBrowseVisitAsync(object? webView, string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (url.Contains("josephy.ir", StringComparison.OrdinalIgnoreCase))
            {
                await TrackVisitAsync(AppConstants.PartialVisitKeys.JosephyBrowse);
                return;
            }

            if (webView is not WebView2 wv2 || wv2.IsDisposed || wv2.CoreWebView2 == null)
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return;

            if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                uri.Host.Equals(AppConstants.Urls.OfflineVirtualHost, StringComparison.OrdinalIgnoreCase))
                return;

            bool isKopolart = uri.Host.Equals(AppConstants.Urls.Domain, StringComparison.OrdinalIgnoreCase) ||
                              uri.Host.EndsWith("." + AppConstants.Urls.Domain, StringComparison.OrdinalIgnoreCase);

            if (!isKopolart)
                return;

            var now = DateTime.UtcNow;
            lock (_lock)
            {
                if (now - _lastTrackedUtc < CooldownPeriod)
                    return;

                _lastTrackedUtc = now;
            }

            try
            {
                string script = "(function() {" +
                    "try {" +
                    "fetch('/add-visit-app/" + AppConstants.PartialVisitKeys.WindowsAppBrowse + "', { method: 'POST', cache: 'no-store' }).catch(function() {});" +
                    "} catch (e) {}" +
                    "})();";

                if (wv2.InvokeRequired)
                {
                    wv2.BeginInvoke(async () =>
                    {
                        try
                        {
                            if (wv2 is { IsDisposed: false, CoreWebView2: not null })
                            {
                                await wv2.CoreWebView2.ExecuteScriptAsync(script);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[AppVisitTrackerService] WebView2 script execution error: {ex.Message}");
                        }
                    });
                }
                else
                {
                    await wv2.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppVisitTrackerService] Tracking error: {ex.Message}");
            }
        }

        public async Task TrackVisitAsync(string visitId)
        {
            if (string.IsNullOrWhiteSpace(visitId))
                return;

            var now = DateTime.UtcNow;
            if (_lastTrackedMap.TryGetValue(visitId, out var lastTime) && (now - lastTime).TotalSeconds < 15)
            {
                return;
            }
            _lastTrackedMap[visitId] = now;

            try
            {
                var requestUrl = $"https://{AppConstants.Urls.Domain}/add-visit-app/{Uri.EscapeDataString(visitId)}";
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, requestUrl);
                _ = await TrackingHttpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppVisitTrackerService] Visit tracking error for '{visitId}': {ex.Message}");
            }
        }
    }
}
