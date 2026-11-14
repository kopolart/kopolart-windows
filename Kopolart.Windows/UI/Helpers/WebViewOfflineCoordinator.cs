using Kopolart.Windows.Domain.Constants;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;

namespace Kopolart.Windows.UI.Helpers
{
    public class WebViewOfflineCoordinator
    {
        public bool EnableOfflinePage { get; set; } = true;
        public bool IsShowingOfflinePage { get; set; }
        public bool OfflineHostMappingSet { get; set; }
        public ulong? OfflineNavigationId { get; set; }

        public void Reset()
        {
            IsShowingOfflinePage = false;
            OfflineHostMappingSet = false;
            OfflineNavigationId = null;
        }

        public void ShowOfflinePage(CoreWebView2? coreWebView2, bool isDarkMode)
        {
            if (coreWebView2 == null)
                return;

            var assetsFolder = OfflinePageManager.TryEnsureAssetsExtracted();
            if (assetsFolder == null)
                return;

            try
            {
                if (!OfflineHostMappingSet)
                {
                    coreWebView2.SetVirtualHostNameToFolderMapping(
                        AppConstants.Urls.OfflineVirtualHost,
                        assetsFolder,
                        CoreWebView2HostResourceAccessKind.DenyCors);
                    OfflineHostMappingSet = true;
                }

                IsShowingOfflinePage = true;
                coreWebView2.Navigate(OfflinePageManager.BuildOfflineUrl(isDarkMode));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show offline page: {ex.Message}");
            }
        }

        public static bool IsOfflineRelatedError(CoreWebView2WebErrorStatus status)
        {
            return status is
                CoreWebView2WebErrorStatus.ConnectionAborted or
                CoreWebView2WebErrorStatus.CannotConnect or
                CoreWebView2WebErrorStatus.HostNameNotResolved or
                CoreWebView2WebErrorStatus.Timeout or
                CoreWebView2WebErrorStatus.Disconnected or
                CoreWebView2WebErrorStatus.ServerUnreachable;
        }

        public string GetDocumentTitle(CoreWebView2? coreWebView2)
        {
            if (IsShowingOfflinePage)
            {
                return TextManager.GetString("OfflineHeading");
            }
            return coreWebView2?.DocumentTitle ?? string.Empty;
        }
    }
}

