using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class OnlineUsersService : IOnlineUsersService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        private readonly HttpClient _httpClient;
        private readonly ILogger<OnlineUsersService>? _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private int? _cachedCount;
        private DateTime _lastFetchUtc = DateTime.MinValue;

        public OnlineUsersService(IHttpClientFactory httpClientFactory, ILogger<OnlineUsersService>? logger = null)
        {
            _httpClient = httpClientFactory?.CreateClient("DefaultClient") ?? new HttpClient();
            _logger = logger;
        }

        public async Task<int?> GetOnlineUsersCountAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            // 1. Check in-memory cache if not forced
            if (!forceRefresh && _cachedCount.HasValue && (DateTime.UtcNow - _lastFetchUtc) < CacheDuration)
            {
                return _cachedCount;
            }

            // 2. Offline check - if no network, return cached value immediately
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    return _cachedCount;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "NetworkInterface check skipped.");
            }

            // 3. Acquire lock to prevent duplicate parallel requests
            if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false))
            {
                return _cachedCount;
            }

            try
            {
                // Double check after lock
                if (!forceRefresh && _cachedCount.HasValue && (DateTime.UtcNow - _lastFetchUtc) < CacheDuration)
                {
                    return _cachedCount;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(RequestTimeout);

                // Cache-busting URL to prevent WinHttp / proxy caching of GET response
                var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var requestUrl = $"{AppConstants.Urls.TopBarInfoUrl}?_t={cacheBuster}";

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true,
                    MaxAge = TimeSpan.Zero
                };
                request.Headers.Pragma.ParseAdd("no-cache");

                using var responseMessage = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    return _cachedCount;
                }

                var response = await responseMessage.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(response))
                {
                    return _cachedCount;
                }

                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("online", out var onlineProp) && onlineProp.TryGetInt32(out int onlineVal))
                {
                    _cachedCount = onlineVal;
                    _lastFetchUtc = DateTime.UtcNow;
                    return _cachedCount;
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("Online users request timed out.");
            }
            catch (Exception ex)
            {
                // Never throw or crash the app on offline or server issues
                _logger?.LogWarning(ex, "Failed to fetch online users count from server.");
                Debug.WriteLine($"[OnlineUsersService] Warning: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }

            return _cachedCount;
        }
    }
}
