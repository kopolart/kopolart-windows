using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Infrastructure.Security;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class WebViewService(ISettingsManager? settingsManager = null) : IWebViewService
    {
        private readonly ISettingsManager? _settingsManager = settingsManager;

        // No total timeout: the runtime installer is ~180 MB and must be downloadable on slow
        // connections. Stalls are detected via the per-read inactivity timeout below instead.
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        private static readonly TimeSpan DownloadInactivityTimeout = TimeSpan.FromSeconds(90);

        public async Task InitializeAsync(object webView)
        {
            if (webView is not WebView2 wv2)
            {
                throw new ArgumentException($"Expected type {typeof(WebView2).FullName}, but got {webView?.GetType().FullName}", nameof(webView));
            }

            var webView2Environment = await CreateWebView2EnvironmentAsync();
            await wv2.EnsureCoreWebView2Async(webView2Environment);
        }

        /// <summary>
        /// Clears only disk/memory caches and local storage — does NOT remove cookies, so the user stays logged in.
        /// </summary>
        public async Task ClearCacheOnlyAsync(object? webView)
        {
            if (webView is WebView2 wv2 && wv2.CoreWebView2?.Profile != null)
            {
                await wv2.CoreWebView2.Profile.ClearBrowsingDataAsync(
                    CoreWebView2BrowsingDataKinds.CacheStorage |
                    CoreWebView2BrowsingDataKinds.DiskCache |
                    CoreWebView2BrowsingDataKinds.IndexedDb |
                    CoreWebView2BrowsingDataKinds.LocalStorage |
                    CoreWebView2BrowsingDataKinds.WebSql
                );
            }
        }

        /// <summary>
        /// Clears only cookies — the user will be logged out but cached files remain.
        /// </summary>
        public async Task ClearCookiesOnlyAsync(object? webView)
        {
            if (webView is WebView2 wv2 && wv2.CoreWebView2?.Profile != null)
            {
                await wv2.CoreWebView2.Profile.ClearBrowsingDataAsync(
                    CoreWebView2BrowsingDataKinds.Cookies
                );
            }
        }

        /// <summary>
        /// Clears all browsing data, including cache, cookies, local storage, history, autofill, etc.
        /// </summary>
        public async Task ClearAllBrowsingDataAsync(object? webView)
        {
            if (webView is WebView2 wv2 && wv2.CoreWebView2?.Profile != null)
            {
                await wv2.CoreWebView2.Profile.ClearBrowsingDataAsync(
                    CoreWebView2BrowsingDataKinds.CacheStorage |
                    CoreWebView2BrowsingDataKinds.DiskCache |
                    CoreWebView2BrowsingDataKinds.IndexedDb |
                    CoreWebView2BrowsingDataKinds.LocalStorage |
                    CoreWebView2BrowsingDataKinds.WebSql |
                    CoreWebView2BrowsingDataKinds.Cookies
                );
            }
        }

        public async Task<string?> GetCookieValueAsync(object? webView, string cookieName, string? url = null)
        {
            if (webView is WebView2 wv2 && wv2.CoreWebView2?.CookieManager != null)
            {
                try
                {
                    string targetUrl = url ?? AppConstants.Urls.MainUrl;
                    var cookies = await wv2.CoreWebView2.CookieManager.GetCookiesAsync(targetUrl);
                    var match = cookies?.FirstOrDefault(c => string.Equals(c.Name, cookieName, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        return match.Value;
                    }

                    // Fallback: search all cookies in the profile
                    var allCookies = await wv2.CoreWebView2.CookieManager.GetCookiesAsync(null);
                    var fallbackMatch = allCookies?.FirstOrDefault(c => string.Equals(c.Name, cookieName, StringComparison.OrdinalIgnoreCase));
                    return fallbackMatch?.Value;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to get cookie '{cookieName}': {ex.Message}");
                }
            }
            return null;
        }

        public long GetCacheSizeBytes()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppConstants.Paths.AppDataFolderName,
                    AppConstants.Paths.WebView2UserDataFolder);

                if (!Directory.Exists(userDataFolder))
                {
                    return 0;
                }

                long totalBytes = 0;
                var dirInfo = new DirectoryInfo(userDataFolder);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        totalBytes += file.Length;
                    }
                    catch
                    {
                        // Skip locked or inaccessible files
                    }
                }

                return totalBytes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to calculate WebView2 cache size: {ex}");
                return 0;
            }
        }

        public bool IsRuntimeInstalled()
        {
            try
            {
                var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return !string.IsNullOrEmpty(version);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task InstallRuntimeAsync(IProgress<int> progress, Action<string>? statusCallback)
        {
            const string installerUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

            // Prefer the bundled installer (shipped next to the exe) over downloading
            var bundledInstaller = Path.Combine(AppContext.BaseDirectory, AppConstants.Files.WebViewRuntimeExe);
            bool useBundled = File.Exists(bundledInstaller);
            bool downloadedInstaller = false;

            // A randomized temp file name closes the TOCTOU window of a fixed, predictable
            // path being swapped by another process between download and the elevated launch.
            string installerPath = useBundled
                ? bundledInstaller
                : Path.Combine(Path.GetTempPath(), $"MicrosoftEdgeWebView2Setup_{Guid.NewGuid():N}.exe");

            try
            {
                if (!useBundled)
                {
                    // Download the installer from Microsoft CDN
                    statusCallback?.Invoke("Downloading requirements...");

                    // Cancels only when the stream stalls; renewed after every successful read.
                    using var inactivityCts = new CancellationTokenSource(DownloadInactivityTimeout);

                    using (var response = await HttpClient.GetAsync(installerUrl, HttpCompletionOption.ResponseHeadersRead, inactivityCts.Token))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? 10 * 1024 * 1024;

                        await using (var stream = await response.Content.ReadAsStreamAsync(inactivityCts.Token))
                        await using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[8192];
                            var totalRead = 0L;
                            var isMoreToRead = true;

                            while (isMoreToRead)
                            {
                                inactivityCts.CancelAfter(DownloadInactivityTimeout);
                                var read = await stream.ReadAsync(buffer, 0, buffer.Length, inactivityCts.Token);
                                if (read == 0)
                                {
                                    isMoreToRead = false;
                                }
                                else
                                {
                                    await fileStream.WriteAsync(buffer, 0, read, inactivityCts.Token);
                                    totalRead += read;
                                    var percentage = Math.Min((double)totalRead / totalBytes * 100, 100);
                                    progress?.Report((int)percentage);
                                }
                            }
                        }
                    }
                    downloadedInstaller = true;

                    // Never hand an unverified binary to an elevated ("runas") launch.
                    if (!AuthenticodeVerifier.IsTrustedMicrosoftBinary(installerPath))
                    {
                        throw new InvalidOperationException("Downloaded WebView2 installer failed Authenticode signature verification.");
                    }
                }

                progress?.Report(100);
                statusCallback?.Invoke("Please complete the Microsoft Edge WebView2 Setup...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "",
                    Verb = "runas",
                    UseShellExecute = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to install WebView2 runtime: {ex}");
                throw; // Re-throw to let the caller handle the UI feedback
            }
            finally
            {
                // Only clean up if we downloaded a temporary installer — never delete the bundled one
                if (downloadedInstaller)
                {
                    try { File.Delete(installerPath); }
                    catch (Exception ex) { Debug.WriteLine($"Failed to clean up installer: {ex}"); }
                }
            }
        }

        private async Task<CoreWebView2Environment> CreateWebView2EnvironmentAsync()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppConstants.Paths.AppDataFolderName,
                AppConstants.Paths.WebView2UserDataFolder);

            var fixedRuntimeFolder = await Task.Run(() => TryFindFixedWebView2RuntimeFolder(GetWebView2RuntimeSearchRoots(baseDirectory))).ConfigureAwait(true);
            CoreWebView2EnvironmentOptions? options = null;
            if (_settingsManager?.GetDisableGpuAcceleration() == true)
            {
                options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--disable-gpu"
                };
            }

            if (!string.IsNullOrWhiteSpace(fixedRuntimeFolder))
            {
                return await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: fixedRuntimeFolder,
                    userDataFolder: userDataFolder,
                    options: options).ConfigureAwait(true);
            }

            return await CoreWebView2Environment.CreateAsync(
                userDataFolder: userDataFolder,
                options: options).ConfigureAwait(true);
        }

        private IEnumerable<string> GetWebView2RuntimeSearchRoots(string baseDirectory)
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(baseDirectory))
            {
                roots.Add(baseDirectory);
            }

            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                var processDirectory = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrWhiteSpace(processDirectory))
                {
                    roots.Add(processDirectory);
                }
            }

            if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is string nativeSearchDirectories &&
                !string.IsNullOrWhiteSpace(nativeSearchDirectories))
            {
                foreach (var directory in nativeSearchDirectories.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    roots.Add(directory.Trim());
                }
            }

            return roots.Where(Directory.Exists);
        }

        private string? TryFindFixedWebView2RuntimeFolder(IEnumerable<string> searchRoots)
        {
            var candidateDirectories = new List<string>();

            foreach (var root in searchRoots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                candidateDirectories.Add(root);
                candidateDirectories.Add(Path.Combine(root, "WebView2"));
                candidateDirectories.Add(Path.Combine(root, "WebView2Runtime"));
            }

            foreach (var directory in candidateDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                try
                {
                    foreach (var file in Directory.EnumerateFiles(directory, AppConstants.Files.WebViewRuntimeExe, SearchOption.AllDirectories))
                    {
                        return Path.GetDirectoryName(file);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to find WebView2 runtime folder: {ex}");
                }
            }

            return null;
        }
    }
}
