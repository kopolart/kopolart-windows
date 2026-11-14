using Kopolart.Windows.Application.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class UserIpLogger : IUserIpLogger
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingsManager _settingsManager;
        private readonly ILogger<UserIpLogger> _logger;

        public UserIpLogger(ISettingsManager settingsManager, IHttpClientFactory httpClientFactory, ILogger<UserIpLogger> logger)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _httpClient = httpClientFactory.CreateClient("DefaultClient");
            _logger = logger;
        }

        public async Task LogUserIpAsync()
        {
            try
            {
                var ip = await GetPublicIpAsync();
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    _settingsManager.UpdateLastUsageIp(ip);
                    _logger.LogInformation("App Usage - IP: {Ip}", ip);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log user IP.");
            }
        }

        private async Task<string?> GetPublicIpAsync()
        {
            // Try raw IP plain-text endpoints first
            string[] plainTextEndpoints =
            {
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://checkip.amazonaws.com"
            };

            foreach (var url in plainTextEndpoints)
            {
                var ip = await TryGetPlainTextIpAsync(url);
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    return ip;
                }
            }

            // Fallback: JSON endpoints
            var jsonIp = await TryGetJsonIpAsync("https://freeipapi.com/api/json", "ipAddress");
            if (!string.IsNullOrWhiteSpace(jsonIp))
            {
                return jsonIp;
            }

            return null;
        }

        private async Task<string?> TryGetPlainTextIpAsync(string url)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetStringAsync(url, cts.Token);
                var trimmed = response?.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && IPAddress.TryParse(trimmed, out var parsed))
                {
                    return parsed.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to fetch IP from {Url}", url);
            }

            return null;
        }

        private async Task<string?> TryGetJsonIpAsync(string url, string propertyName)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetStringAsync(url, cts.Token);
                if (string.IsNullOrWhiteSpace(response)) return null;

                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty(propertyName, out var ipElem))
                {
                    var raw = ipElem.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(raw) && IPAddress.TryParse(raw, out var parsed))
                    {
                        return parsed.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to fetch IP from JSON API {Url}", url);
            }

            return null;
        }
    }
}
