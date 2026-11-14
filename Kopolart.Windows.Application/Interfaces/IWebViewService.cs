using System;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IWebViewService
    {
        Task InitializeAsync(object webView);
        Task ClearCacheOnlyAsync(object? webView);
        Task ClearCookiesOnlyAsync(object? webView);
        Task ClearAllBrowsingDataAsync(object? webView);
        bool IsRuntimeInstalled();
        Task InstallRuntimeAsync(IProgress<int> progress, Action<string>? statusCallback);
        long GetCacheSizeBytes();
        Task<string?> GetCookieValueAsync(object? webView, string cookieName, string? url = null);
    }
}
